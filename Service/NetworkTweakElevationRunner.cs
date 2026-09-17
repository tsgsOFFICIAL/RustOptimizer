using RustOptimizer.Service.Logging;
using System.Runtime.Versioning;
using System.Collections.Generic;
using RustOptimizer.Interface;

namespace RustOptimizer.Service;

/// <summary>
/// The elevated codepath for <c>--apply-network-tweak &lt;key&gt; &lt;value&gt;</c>, intercepted in
/// <c>Program.Main</c> before the normal DI/Avalonia startup runs. This process is the same exe
/// re-launched via <see cref="ElevationHelper.RunElevated"/> with a UAC prompt already accepted -
/// it applies exactly one tweak and exits, without touching the DI container or opening a window.
/// </summary>
public static class NetworkTweakElevationRunner
{
    /// <summary>
    /// Applies one network tweak. <paramref name="key"/> is one of "NetworkThrottling",
    /// "NicPowerSaving", or "QosReservedBandwidth"; <paramref name="value"/> is "1" to enable/apply
    /// the tweak or "0" to revert it. Returns the process exit code: 0 on success, 1 on failure or
    /// an unrecognized key.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static int Run(string key, string value)
    {
        bool enabled = value == "1";
        INetworkTweaksService service = new NetworkTweaksService();

        bool success = key switch
        {
            "NetworkThrottling" => service.SetNetworkThrottlingDisabled(enabled),
            "NicPowerSaving" => service.SetNicPowerSavingDisabledForActiveAdapter(enabled),
            "QosReservedBandwidth" => service.SetQosReservedBandwidthDisabled(enabled),
            _ => LogUnknownKey(key)
        };

        return success ? 0 : 1;
    }

    /// <summary>
    /// Applies every given network tweak in this one elevated process - one UAC prompt for Smart
    /// Optimization's whole batch instead of one per tweak. Best-effort: a failure on one key
    /// doesn't stop the rest from being attempted. Returns 0 only if every tweak in the batch
    /// succeeded, matching <see cref="Run"/>'s single-tweak contract.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static int RunBatch(IReadOnlyList<(string Key, string Value)> tweaks)
    {
        bool allSucceeded = true;
        foreach ((string key, string value) in tweaks)
            allSucceeded &= Run(key, value) == 0;

        return allSucceeded ? 0 : 1;
    }

    /// <summary>Logs an unrecognized tweak key and reports failure.</summary>
    private static bool LogUnknownKey(string key)
    {
        AppLog.Warn("NetworkTweakElevationRunner", $"Unrecognized network tweak key: '{key}'.");
        return false;
    }
}