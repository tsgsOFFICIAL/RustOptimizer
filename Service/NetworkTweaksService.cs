using System.Net.NetworkInformation;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Net.Http;
using System.Linq;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="INetworkTweaksService" />
/// <remarks>
/// Every setter here writes to <c>HKEY_LOCAL_MACHINE</c>, which requires administrator rights -
/// this class assumes it's only ever asked to write from an already-elevated process (see
/// <see cref="NetworkTweakElevationRunner"/>). The getters and adapter/ping info are plain reads
/// and work fine unelevated.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class NetworkTweaksService : INetworkTweaksService
{
    private const string ReferenceHost = "1.1.1.1";
    private const string MultimediaSystemProfileKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    private const string PschedKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\Psched";
    private const string NetworkClassKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    // Tried in order until one succeeds - some networks (corporate filtering in particular) block
    // individual "what's my IP" services outright, so relying on just one is fragile. All three
    // return a bare IP string with no JSON wrapping.
    private static readonly string[] PublicIpEndpoints =
    [
        "https://api.ipify.org?format=text",
        "https://icanhazip.com",
        "https://ifconfig.me/ip"
    ];

    // Windows' documented default when NetworkThrottlingIndex is absent - disabling the throttle
    // means writing the special "no limit" DWORD instead.
    private const int NetworkThrottlingDisabledDword = unchecked((int)0xffffffff);

    // A community-documented convention for the "Allow the computer to turn off this device to
    // save power" checkbox, not from an authoritative Microsoft source - verify against a real
    // device before relying on it.
    private const int PnpCapabilitiesPowerSavingDisabled = 24;

    // A single shared, long-lived instance.
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <inheritdoc />
    public NetworkAdapterInfo? GetActiveAdapterInfo()
    {
        NetworkInterface? adapter = GetActiveAdapter();
        if (adapter is null)
            return null;

        IPInterfaceProperties ipProperties = adapter.GetIPProperties();
        string? ipv4Address = ipProperties.UnicastAddresses
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
        string? gatewayAddress = ipProperties.GatewayAddresses
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
        IReadOnlyList<string> dnsServers = ipProperties.DnsAddresses
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.ToString())
            .ToList();

        return new NetworkAdapterInfo(adapter.Name, adapter.Description, adapter.Speed,
            ipv4Address, gatewayAddress, FormatMacAddress(adapter.GetPhysicalAddress()), dnsServers,
            IsWireless: adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
    }

    /// <inheritdoc />
    public NetworkByteCounters? GetActiveAdapterByteCounters()
    {
        NetworkInterface? adapter = GetActiveAdapter();
        if (adapter is null)
            return null;

        IPInterfaceStatistics stats = adapter.GetIPStatistics();
        return new NetworkByteCounters(stats.BytesSent, stats.BytesReceived);
    }

    /// <inheritdoc />
    public async Task<long?> PingReferenceHostAsync()
    {
        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(ReferenceHost, 2000);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch (Exception ex)
        {
            AppLog.Warn("NetworkTweaksService", "Failed to ping the reference host.", ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetPublicIpAddressAsync()
    {
        foreach (string endpoint in PublicIpEndpoints)
        {
            try
            {
                string ip = (await HttpClient.GetStringAsync(endpoint)).Trim();
                if (ip.Length > 0)
                    return ip;
            }
            catch (Exception ex)
            {
                AppLog.Warn("NetworkTweaksService", $"Failed to fetch the public IP address from '{endpoint}'.", ex);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public NetworkTweaksSettings GetNetworkTweaksSettings()
    {
        string? adapterGuid = GetActiveAdapter()?.Id;
        return new NetworkTweaksSettings(
            NetworkThrottlingDisabled: ReadDword(MultimediaSystemProfileKeyPath, "NetworkThrottlingIndex") == NetworkThrottlingDisabledDword,
            NicPowerSavingDisabled: adapterGuid is null ? null : ReadNicPowerSavingDisabled(adapterGuid),
            QosReservedBandwidthDisabled: ReadDword(PschedKeyPath, "NonBestEffortLimit") == 0);
    }

    /// <inheritdoc />
    public bool SetNetworkThrottlingDisabled(bool disabled) =>
        disabled
            ? WriteDword(MultimediaSystemProfileKeyPath, "NetworkThrottlingIndex", NetworkThrottlingDisabledDword)
            : DeleteValue(MultimediaSystemProfileKeyPath, "NetworkThrottlingIndex");

    /// <inheritdoc />
    public bool SetNicPowerSavingDisabledForActiveAdapter(bool disabled)
    {
        string? adapterGuid = GetActiveAdapter()?.Id;
        string? classSubkeyPath = adapterGuid is null ? null : FindClassSubkeyPath(adapterGuid);
        if (classSubkeyPath is null)
            return false;

        return disabled
            ? WriteDword(classSubkeyPath, "PnPCapabilities", PnpCapabilitiesPowerSavingDisabled)
            : DeleteValue(classSubkeyPath, "PnPCapabilities");
    }

    /// <inheritdoc />
    public bool SetQosReservedBandwidthDisabled(bool disabled) =>
        disabled
            ? WriteDword(PschedKeyPath, "NonBestEffortLimit", 0)
            : DeleteValue(PschedKeyPath, "NonBestEffortLimit");

    /// <summary>
    /// Resolves the network adapter this app treats as "active": operational, not a loopback/tunnel
    /// pseudo-adapter, preferring whichever one has a default gateway. Falls back to the first
    /// remaining operational adapter if none has a gateway - a known simplification for multi-homed
    /// machines (e.g. VPN and physical NIC both up at once).
    /// </summary>
    private static NetworkInterface? GetActiveAdapter() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .OrderByDescending(HasGateway)
            .FirstOrDefault();

    /// <summary>Whether the given adapter reports at least one default gateway.</summary>
    private static bool HasGateway(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPProperties().GatewayAddresses.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads whether NIC power-saving is disabled for the given adapter GUID, or
    /// <see langword="null"/> if its driver's Class subkey couldn't be resolved.
    /// </summary>
    private static bool? ReadNicPowerSavingDisabled(string adapterGuid)
    {
        string? classSubkeyPath = FindClassSubkeyPath(adapterGuid);
        return classSubkeyPath is null ? null : ReadDword(classSubkeyPath, "PnPCapabilities") == PnpCapabilitiesPowerSavingDisabled;
    }

    /// <summary>
    /// Finds the given adapter's numbered driver Class subkey (keyed by enumeration index, not by
    /// GUID) by matching its <c>NetCfgInstanceId</c> value against <paramref name="adapterGuid"/>.
    /// </summary>
    private static string? FindClassSubkeyPath(string adapterGuid)
    {
        try
        {
            using RegistryKey? classKey = Registry.LocalMachine.OpenSubKey(NetworkClassKeyPath);
            if (classKey is null)
                return null;

            string normalizedGuid = NormalizeGuid(adapterGuid);
            foreach (string subkeyName in classKey.GetSubKeyNames())
            {
                using RegistryKey? subkey = classKey.OpenSubKey(subkeyName);
                if (subkey?.GetValue("NetCfgInstanceId") is string instanceId && NormalizeGuid(instanceId) == normalizedGuid)
                    return $@"{NetworkClassKeyPath}\{subkeyName}";
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("NetworkTweaksService", "Failed to resolve the active adapter's driver Class subkey.", ex);
        }

        return null;
    }

    /// <summary>Strips braces and normalizes casing so GUIDs from different sources compare equal.</summary>
    private static string NormalizeGuid(string guid) => guid.Trim('{', '}').ToUpperInvariant();

    /// <summary>Formats a MAC address as e.g. "AA:BB:CC:DD:EE:FF", or "" if the adapter has none.</summary>
    private static string FormatMacAddress(PhysicalAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 0 ? "" : string.Join(":", bytes.Select(b => b.ToString("X2")));
    }

    /// <summary>Reads a DWORD registry value under HKLM, or <see langword="null"/> if it's missing or unreadable.</summary>
    private static int? ReadDword(string keyPath, string valueName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
            return key?.GetValue(valueName) is int value ? value : null;
        }
        catch (Exception ex)
        {
            AppLog.Warn("NetworkTweaksService", $"Failed to read '{valueName}' from the registry.", ex);
            return null;
        }
    }

    /// <summary>Writes a DWORD registry value under HKLM, creating the key if needed. Returns whether it succeeded.</summary>
    private static bool WriteDword(string keyPath, string valueName, int value)
    {
        try
        {
            using RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath, writable: true);
            key.SetValue(valueName, value, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("NetworkTweaksService", $"Failed to write '{valueName}' to the registry.", ex);
            return false;
        }
    }

    /// <summary>
    /// Deletes a registry value under HKLM if present, restoring Windows' implicit default for it.
    /// Returns whether it succeeded (missing key/value counts as success - there's nothing to delete).
    /// </summary>
    private static bool DeleteValue(string keyPath, string valueName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("NetworkTweaksService", $"Failed to delete '{valueName}' from the registry.", ex);
            return false;
        }
    }
}