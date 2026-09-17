using RustOptimizer.Interface;

namespace RustOptimizer.Service;

/// <summary>One recommended graphics preset, with the localization key/args for explaining why.</summary>
public readonly record struct RecommendedPreset(ConfigPreset Preset, string ReasonKey, double InstalledMemoryGb, int? MaxRefreshRateHz);

/// <summary>
/// Picks a <see cref="ConfigPreset"/> to suggest for Smart Optimization, built entirely from
/// signals the app already reads elsewhere for the System page/score
/// (<see cref="ISystemInfoService.GetInstalledMemoryBytes"/>, <see cref="ISystemInfoService.GetDisplayModeInfo"/>)
/// rather than probing anything new. Deliberately simple and explainable over precise: there's no
/// GPU benchmark database here, just the same "is this machine below spec" and "does the display
/// support a high frame rate" questions the System page's own warnings already ask.
/// </summary>
internal static class GraphicsPresetRecommendation
{
    // Mirrors SystemOptimizationRecommendations' own "16 GB is the recommended minimum" threshold,
    // so the two don't disagree about what counts as a low-spec machine.
    private const ulong MinimumRecommendedMemoryBytes = 16UL * 1024 * 1024 * 1024;

    // A monitor at or above this refresh rate can actually make use of Competitive's uncapped frame
    // rate; below it, Cinematic's extra visual quality goes noticed for free.
    private const int HighRefreshRateThresholdHz = 120;

    /// <summary>
    /// Recommends Low-End PC when installed memory is below the recommended minimum, Competitive
    /// when the display supports a high refresh rate, otherwise Cinematic.
    /// </summary>
    public static RecommendedPreset Recommend(ulong installedMemoryBytes, int? maxRefreshRateHz)
    {
        double installedMemoryGb = installedMemoryBytes / (1024.0 * 1024.0 * 1024.0);

        ConfigPreset preset = installedMemoryBytes < MinimumRecommendedMemoryBytes
            ? ConfigPreset.LowEndPc
            : maxRefreshRateHz is { } hz && hz >= HighRefreshRateThresholdHz
                ? ConfigPreset.Competitive
                : ConfigPreset.Cinematic;

        string reasonKey = preset switch
        {
            ConfigPreset.LowEndPc => "PresetRecommendationReasonLowEndPc",
            ConfigPreset.Competitive => "PresetRecommendationReasonCompetitive",
            _ => "PresetRecommendationReasonCinematic"
        };

        return new RecommendedPreset(preset, reasonKey, installedMemoryGb, maxRefreshRateHz);
    }
}