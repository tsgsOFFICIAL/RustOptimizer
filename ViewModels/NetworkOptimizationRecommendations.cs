using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Everything <see cref="NetworkOptimizationRecommendations.Score"/> needs to tally the Network
/// category. <see cref="NicPowerSavingDisabled"/> is <see langword="null"/> when no active adapter
/// (or its driver Class subkey) could be resolved, same as <see cref="NetworkTweaksSettings"/>'s
/// own nullable fields. <see cref="IsWireless"/> is <see langword="null"/> when no active adapter
/// could be resolved at all - it's informational (the app can't fix your connection type), same as
/// <see cref="SystemOptimizationInputs.RustDriveFreePercent"/>-style checks on the System page.
/// </summary>
public readonly record struct NetworkOptimizationInputs(
    bool? NetworkThrottlingDisabled,
    bool? NicPowerSavingDisabled,
    bool? QosReservedBandwidthDisabled,
    bool? IsWireless);

/// <summary>
/// The recommended value for each Network-page tweak this app can score. Mirrors
/// <see cref="SystemOptimizationRecommendations"/>'s shape so future Dashboard/Optimizer-page
/// wiring can consume it the same way.
/// </summary>
public static class NetworkOptimizationRecommendations
{
    /// <summary>Disabling network throttling is recommended - it removes an artificial per-flow bandwidth cap.</summary>
    public static bool IsNetworkThrottlingRecommended(bool disabled) => disabled;

    /// <summary>Disabling NIC power-saving is recommended - it stops Windows from throttling the adapter to save power.</summary>
    public static bool IsNicPowerSavingRecommended(bool disabled) => disabled;

    /// <summary>Disabling QoS's reserved bandwidth limit is recommended so Windows doesn't hold back bandwidth from other traffic.</summary>
    public static bool IsQosReservedBandwidthRecommended(bool disabled) => disabled;

    /// <summary>A wired (Ethernet) connection is recommended - it has lower and far more consistent latency than Wi-Fi.</summary>
    public static bool IsWiredConnectionRecommended(bool isWireless) => !isWireless;

    /// <summary>
    /// Every applicable check for the given inputs, paired with whether it's currently at its
    /// recommended value and the localization key for its short display name. The NIC
    /// power-saving and wired-connection checks are only included when their underlying data is
    /// available. One definition shared by <see cref="Score"/> and <see cref="GetOutstandingLabelKeys"/>
    /// so they can never disagree.
    /// </summary>
    private static IEnumerable<(bool Recommended, string LabelKey)> EvaluateChecks(NetworkOptimizationInputs inputs)
    {
        if (inputs.NetworkThrottlingDisabled is { } networkThrottlingDisabled)
            yield return (IsNetworkThrottlingRecommended(networkThrottlingDisabled), "NetworkThrottlingLabel");

        if (inputs.NicPowerSavingDisabled is { } nicPowerSavingDisabled)
            yield return (IsNicPowerSavingRecommended(nicPowerSavingDisabled), "NicPowerSavingLabel");

        if (inputs.QosReservedBandwidthDisabled is { } qosDisabled)
            yield return (IsQosReservedBandwidthRecommended(qosDisabled), "QosReservedBandwidthLabel");

        if (inputs.IsWireless is { } isWireless)
            yield return (IsWiredConnectionRecommended(isWireless), "WiredConnectionRecommendedLabel");
    }

    /// <summary>Scores every applicable check - how many are at their recommended value, out of how many apply.</summary>
    public static OptimizationCategoryScore Score(NetworkOptimizationInputs inputs)
    {
        int total = 0;
        int optimized = 0;

        foreach ((bool recommended, _) in EvaluateChecks(inputs))
        {
            total++;
            if (recommended) optimized++;
        }

        return new OptimizationCategoryScore(optimized, total);
    }

    /// <summary>
    /// The localization keys of every applicable check that isn't currently at its recommended
    /// value, in the same fixed order <see cref="Score"/> tallies them.
    /// </summary>
    public static IReadOnlyList<string> GetOutstandingLabelKeys(NetworkOptimizationInputs inputs) =>
        EvaluateChecks(inputs).Where(check => !check.Recommended).Select(check => check.LabelKey).ToList();
}