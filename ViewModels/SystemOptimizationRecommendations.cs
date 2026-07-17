using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>The three-stage read on an <see cref="OptimizationCategoryScore"/>, driving each Optimization Overview tile's color.</summary>
public enum OptimizationStatus
{
    /// <summary>None of the applicable checks are at their recommended value.</summary>
    NotOptimized,
    /// <summary>Some, but not all, applicable checks are at their recommended value.</summary>
    PartiallyOptimized,
    /// <summary>Every applicable check is at its recommended value.</summary>
    Optimized
}

/// <summary>
/// One category's optimization tally for the Dashboard's Optimization Overview - how many of its
/// checks currently match their recommended value, out of how many apply.
/// </summary>
public readonly record struct OptimizationCategoryScore(int Optimized, int Total)
{
    /// <summary>The three-stage status derived from <see cref="Optimized"/>/<see cref="Total"/>.</summary>
    public OptimizationStatus Status => Optimized switch
    {
        0 => OptimizationStatus.NotOptimized,
        _ when Optimized == Total => OptimizationStatus.Optimized,
        _ => OptimizationStatus.PartiallyOptimized
    };

    /// <summary>Formatted as "3 / 5 settings".</summary>
    public string SettingsText => $"{Optimized} / {Total} settings";
}

/// <summary>
/// Everything <see cref="SystemOptimizationRecommendations.Score"/> needs to tally the System
/// category. <see cref="RustDriveFreePercent"/> is <see langword="null"/> when Rust isn't
/// installed or its drive couldn't be resolved, same as <see cref="GamingTweaksSettings.FullscreenOptimizationsDisabledForRust"/>.
/// </summary>
public readonly record struct SystemOptimizationInputs(
    GamingTweaksSettings Gaming,
    string? ActivePowerPlanId,
    MemoryInfo Memory,
    MemorySpeedInfo MemorySpeed,
    double? RustDriveFreePercent,
    DisplayModeInfo Display);

/// <summary>
/// The recommended value for each System-page setting this app can score. Shared between
/// <see cref="SystemViewModel"/> (per-row warning icons) and <see cref="DashboardViewModel"/>
/// (the Optimization Overview's System tile), so both agree on one definition of "recommended."
/// </summary>
public static class SystemOptimizationRecommendations
{
    // Windows' built-in high-performance power plan IDs. Anything else - including custom plans -
    // just isn't counted as a known-good match; it isn't assumed bad.
    private const string HighPerformancePlanId = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string UltimatePerformancePlanId = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private const double MinimumFreeStoragePercent = 10.0;
    private const ulong MinimumRecommendedMemoryBytes = 16UL * 1024 * 1024 * 1024;

    /// <summary>Mouse acceleration off is recommended - it makes aim less consistent.</summary>
    public static bool IsPointerPrecisionRecommended(bool enabled) => !enabled;

    /// <summary>Game Mode on is recommended, matching Windows' own default guidance.</summary>
    public static bool IsGameModeRecommended(bool enabled) => enabled;

    /// <summary>Background recording off is recommended to avoid its overhead while playing.</summary>
    public static bool IsBackgroundRecordingRecommended(bool enabled) => !enabled;

    /// <summary>Disabling fullscreen optimizations for Rust is recommended to reduce input latency.</summary>
    public static bool IsFullscreenOptimizationsRecommended(bool disabledForRust) => disabledForRust;

    /// <summary>High performance or Ultimate Performance is recommended over Balanced/Power saver/custom plans.</summary>
    public static bool IsPowerPlanRecommended(string? planId) =>
        string.Equals(planId, HighPerformancePlanId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(planId, UltimatePerformancePlanId, StringComparison.OrdinalIgnoreCase);

    /// <summary>At least 16 GB of physical RAM is recommended for Rust.</summary>
    public static bool IsMemorySizeRecommended(ulong totalBytes) => totalBytes >= MinimumRecommendedMemoryBytes;

    /// <summary>RAM running at its rated speed is recommended - anything lower means its XMP/EXPO profile isn't enabled in BIOS.</summary>
    public static bool IsMemorySpeedRecommended(int currentMhz, int ratedMhz) => currentMhz >= ratedMhz;

    /// <summary>At least 10% free space on Rust's drive is recommended, since Rust's updates need room to download and unpack.</summary>
    public static bool IsStorageSpaceRecommended(double freePercent) => freePercent >= MinimumFreeStoragePercent;

    /// <summary>Running at the monitor's own maximum refresh rate is recommended - anything lower leaves smoothness on the table.</summary>
    public static bool IsRefreshRateRecommended(int currentHz, int maxHz) => currentHz >= maxHz;

    /// <summary>Running at the monitor's own maximum (typically native) resolution is recommended - anything lower leaves clarity on the table.</summary>
    public static bool IsResolutionRecommended(int currentWidth, int currentHeight, int maxWidth, int maxHeight) =>
        currentWidth >= maxWidth && currentHeight >= maxHeight;

    /// <summary>
    /// Finds the free-space percentage of the drive Rust is installed on, or <see langword="null"/>
    /// if Rust isn't installed or that drive isn't among <paramref name="storageDevices"/>.
    /// </summary>
    public static double? FindRustDriveFreePercent(string? rustInstallPath, IReadOnlyList<StorageDeviceInfo> storageDevices)
    {
        string? driveRoot = rustInstallPath != null ? Path.GetPathRoot(rustInstallPath)?.TrimEnd('\\') : null;
        if (driveRoot == null)
            return null;

        LogicalDriveInfo? drive = storageDevices
            .SelectMany(device => device.Drives)
            .Where(d => string.Equals(d.Name, driveRoot, StringComparison.OrdinalIgnoreCase))
            .Select(d => (LogicalDriveInfo?)d)
            .FirstOrDefault();

        return drive is { TotalBytes: > 0 } found ? found.FreeBytes / (double)found.TotalBytes * 100.0 : null;
    }

    /// <summary>
    /// Every applicable check for the given inputs, paired with whether it's currently at its
    /// recommended value and the localization key for its short display name. The fullscreen-
    /// optimizations, memory-speed, storage-space, refresh-rate, and resolution checks are only
    /// included when their underlying data is available (Rust not installed, the OS/WMI didn't
    /// report a rated memory speed, or the display driver didn't report a mode all leave them out).
    /// One definition shared by <see cref="Score"/> and
    /// <see cref="GetOutstandingLabelKeys"/> so they can never disagree.
    /// </summary>
    private static IEnumerable<(bool Recommended, string LabelKey)> EvaluateChecks(SystemOptimizationInputs inputs)
    {
        yield return (IsPointerPrecisionRecommended(inputs.Gaming.PointerPrecisionEnabled), "PointerPrecisionLabel");
        yield return (IsGameModeRecommended(inputs.Gaming.GameModeEnabled), "GameModeLabel");
        yield return (IsBackgroundRecordingRecommended(inputs.Gaming.BackgroundRecordingEnabled), "BackgroundRecordingLabel");
        yield return (IsPowerPlanRecommended(inputs.ActivePowerPlanId), "PowerPlanTitle");
        yield return (IsMemorySizeRecommended(inputs.Memory.TotalBytes), "RamLabel");

        if (inputs.Gaming.FullscreenOptimizationsDisabledForRust is { } disabled)
            yield return (IsFullscreenOptimizationsRecommended(disabled), "FullscreenOptimizationsLabel");

        if (inputs.MemorySpeed is { CurrentMhz: { } currentMhz, RatedMhz: { } ratedMhz })
            yield return (IsMemorySpeedRecommended(currentMhz, ratedMhz), "MemorySpeedLabel");

        if (inputs.RustDriveFreePercent is { } freePercent)
            yield return (IsStorageSpaceRecommended(freePercent), "StorageTitle");

        if (inputs.Display.CurrentHz is { } currentHz && inputs.Display.MaxHz is { } maxHz)
            yield return (IsRefreshRateRecommended(currentHz, maxHz), "RefreshRateLabel");

        if (inputs.Display is { CurrentWidth: { } currentWidth, CurrentHeight: { } currentHeight, MaxWidth: { } maxWidth, MaxHeight: { } maxHeight })
            yield return (IsResolutionRecommended(currentWidth, currentHeight, maxWidth, maxHeight), "ResolutionLabel");
    }

    /// <summary>Scores every applicable check - how many are at their recommended value, out of how many apply.</summary>
    public static OptimizationCategoryScore Score(SystemOptimizationInputs inputs)
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
    /// value, in the same fixed order <see cref="Score"/> tallies them - for a short "what's wrong"
    /// summary without sending the user hunting through the System page's warning icons.
    /// </summary>
    public static IReadOnlyList<string> GetOutstandingLabelKeys(SystemOptimizationInputs inputs) =>
        EvaluateChecks(inputs).Where(check => !check.Recommended).Select(check => check.LabelKey).ToList();
}