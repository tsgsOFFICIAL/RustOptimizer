using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Everything <see cref="GameplayOptimizationRecommendations.Score"/> needs to tally the Gameplay
/// category. <see cref="Tweaks"/> comes from <see cref="IConfigService.GetRecommendedGameplayTweaks"/>
/// and <see cref="CurrentConvars"/> from a fresh <see cref="IConfigService.ReadConvars"/> of every
/// convar those tweaks touch - the same way <see cref="GameplayViewModel"/> checks each row.
/// </summary>
public readonly record struct GameplayOptimizationInputs(IReadOnlyList<GameplayTweak> Tweaks, IReadOnlyDictionary<string, string> CurrentConvars);

/// <summary>
/// Scores only the Gameplay page's "recommended for everyone" tweaks - the ones with no real
/// downside for anyone, same set <see cref="RustOptimizer.Service.RecommendedGameplayTweaks"/>
/// tags <see cref="GameplayTweakCategory.RecommendedForEveryone"/>. "Preference" tweaks (crosshair
/// style, world notifications, etc.) are deliberately left out - there's no universally "right"
/// value for them, same reasoning <see cref="NetworkOptimizationRecommendations"/> excludes
/// wired-vs-wireless from anything it would auto-fix. Mirrors
/// <see cref="SystemOptimizationRecommendations"/>'s shape so Dashboard/Optimizer-page wiring can
/// consume all three categories uniformly.
/// </summary>
public static class GameplayOptimizationRecommendations
{
    /// <summary>Whether every convar a tweak sets is currently at its recommended (enabled) value - the same check <see cref="GameplayViewModel"/> uses per row.</summary>
    public static bool IsTweakApplied(GameplayTweak tweak, IReadOnlyDictionary<string, string> current) =>
        tweak.Convars.All(c => current.TryGetValue(c.Convar, out string? value) && string.Equals(value, c.EnabledValue, StringComparison.OrdinalIgnoreCase));

    /// <summary>Every "recommended for everyone" tweak, paired with whether it's currently applied and its localization key.</summary>
    private static IEnumerable<(bool Recommended, string LabelKey)> EvaluateChecks(GameplayOptimizationInputs inputs) =>
        inputs.Tweaks
            .Where(t => t.Category == GameplayTweakCategory.RecommendedForEveryone)
            .Select(t => (IsTweakApplied(t, inputs.CurrentConvars), t.LabelKey));

    /// <summary>Scores every "recommended for everyone" tweak - how many are currently applied, out of how many there are.</summary>
    public static OptimizationCategoryScore Score(GameplayOptimizationInputs inputs)
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

    /// <summary>The localization keys of every "recommended for everyone" tweak that isn't currently applied, in display order.</summary>
    public static IReadOnlyList<string> GetOutstandingLabelKeys(GameplayOptimizationInputs inputs) =>
        EvaluateChecks(inputs).Where(check => !check.Recommended).Select(check => check.LabelKey).ToList();
}