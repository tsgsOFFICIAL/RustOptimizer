using System.Collections.Generic;
using System.Threading.Tasks;

namespace RustOptimizer.Interface;

/// <summary>
/// The active network adapter's identity, link speed, and local network configuration.
/// <see cref="IPv4Address"/>/<see cref="GatewayAddress"/> are <see langword="null"/> if the
/// adapter has no IPv4 configuration; <see cref="DnsServers"/> is empty if none are configured
/// (e.g. DHCP hasn't supplied any yet).
/// </summary>
public readonly record struct NetworkAdapterInfo(
    string Name,
    string Description,
    long LinkSpeedBps,
    string? IPv4Address,
    string? GatewayAddress,
    string MacAddress,
    IReadOnlyList<string> DnsServers,
    bool IsWireless);

/// <summary>The active adapter's cumulative sent/received byte counters, for computing a live throughput rate between two samples.</summary>
public readonly record struct NetworkByteCounters(long BytesSent, long BytesReceived);

/// <summary>
/// The subset of network-related Windows settings this app can toggle. Every value is
/// <see langword="null"/> when it couldn't be read (e.g. no active adapter could be resolved for
/// the per-adapter tweaks), the same convention <see cref="GamingTweaksSettings"/> uses for
/// <see cref="GamingTweaksSettings.FullscreenOptimizationsDisabledForRust"/>.
/// </summary>
public readonly record struct NetworkTweaksSettings(
    bool? NetworkThrottlingDisabled,
    bool? NicPowerSavingDisabled,
    bool? QosReservedBandwidthDisabled);

/// <summary>
/// Reads and applies OS-level network tweaks, and reports basic active-adapter/connectivity
/// info. Unlike <see cref="ISystemTweaksService"/>, every setter here writes to
/// <c>HKEY_LOCAL_MACHINE</c>, which requires administrator rights - callers are expected to run
/// the setters only from an elevated process (see <c>ElevationHelper</c>), while the getters and
/// adapter/ping info are safe to call unelevated.
/// </summary>
public interface INetworkTweaksService
{
    /// <summary>Gets the active network adapter's identity, link speed, and local network configuration, or <see langword="null"/> if none could be resolved.</summary>
    NetworkAdapterInfo? GetActiveAdapterInfo();

    /// <summary>Gets the active adapter's cumulative sent/received byte counters, or <see langword="null"/> if none could be resolved.</summary>
    NetworkByteCounters? GetActiveAdapterByteCounters();

    /// <summary>Pings a fixed reference host (1.1.1.1) once and returns the round-trip time in milliseconds, or <see langword="null"/> on failure.</summary>
    Task<long?> PingReferenceHostAsync();

    /// <summary>
    /// Looks up this machine's public-facing IP address via a third-party lookup service, since
    /// that isn't something Windows or the local adapter configuration can report on their own.
    /// Tries a short list of providers in order, since some networks block individual ones.
    /// Returns <see langword="null"/> if every provider fails (offline, all blocked, etc.).
    /// </summary>
    Task<string?> GetPublicIpAddressAsync();

    /// <summary>Gets the current state of every network tweak this service can toggle.</summary>
    NetworkTweaksSettings GetNetworkTweaksSettings();

    /// <summary>
    /// Enables/disables the Multimedia Class Scheduler Service's network throttling. Requires
    /// administrator rights. Returns whether it succeeded.
    /// </summary>
    bool SetNetworkThrottlingDisabled(bool disabled);

    /// <summary>
    /// Enables/disables the active adapter's "Allow the computer to turn off this device to save
    /// power" capability. Requires administrator rights; fails if no active adapter could be
    /// resolved. Returns whether it succeeded.
    /// </summary>
    bool SetNicPowerSavingDisabledForActiveAdapter(bool disabled);

    /// <summary>
    /// Enables/disables QoS's reserved bandwidth limit for non-best-effort traffic, machine-wide.
    /// Requires administrator rights. Returns whether it succeeded.
    /// </summary>
    bool SetQosReservedBandwidthDisabled(bool disabled);
}