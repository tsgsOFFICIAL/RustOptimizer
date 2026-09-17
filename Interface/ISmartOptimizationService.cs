using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace RustOptimizer.Interface;

/// <summary>Which page a <see cref="PlannedChange"/> belongs to - drives the confirm dialog's grouping and icon.</summary>
public enum OptimizationCategory
{
    System,
    Network,
    Gameplay
}

/// <summary>
/// One outstanding, actionable tweak Smart Optimization would fix. <see cref="LabelKey"/> is the
/// same short localization key its own page already uses for the setting (e.g. "GameModeLabel"),
/// so the confirm dialog and the category pages always agree on what to call it.
/// <see cref="RequiresElevation"/> is <see langword="true"/> only for Network tweaks - they're the
/// only category whose setters need administrator rights.
/// </summary>
public readonly record struct PlannedChange(OptimizationCategory Category, string LabelKey, bool RequiresElevation);

/// <summary>
/// Everything Smart Optimization is about to do: every outstanding, actionable System/Network/
/// Gameplay tweak, plus - only when client.cfg matches none of the three built-in presets yet - a
/// recommended graphics preset with the localization key/values to explain why it was picked.
/// <see cref="RecommendedGraphicsPreset"/> stays <see langword="null"/> whenever a built-in preset
/// is already applied, so a deliberate graphics choice is never silently overwritten.
/// </summary>
public readonly record struct SmartOptimizationPlan(
    IReadOnlyList<PlannedChange> Changes,
    ConfigPreset? RecommendedGraphicsPreset,
    string? GraphicsPresetReasonKey,
    double GraphicsInstalledMemoryGb,
    int? GraphicsMaxRefreshRateHz,
    bool RustRunning)
{
    /// <summary>Whether there's nothing outstanding at all - every check passes and a graphics preset is already applied (or Rust isn't installed).</summary>
    public bool IsEmpty => Changes.Count == 0 && RecommendedGraphicsPreset is null;

    /// <summary>
    /// Whether Rust needs to be closed before Gameplay/Graphics changes in this plan can actually be
    /// written - both write to client.cfg, which <see cref="IConfigService.SetConvars"/> always
    /// refuses while Rust has it open. Detecting what's outstanding is a safe read either way, so
    /// <see cref="ISmartOptimizationService.BuildPlan"/> still reports them even when this is
    /// <see langword="true"/>, rather than hiding them behind a misleading "already optimized".
    /// </summary>
    public bool RequiresClosingRust => RustRunning && (Changes.Any(c => c.Category == OptimizationCategory.Gameplay) || RecommendedGraphicsPreset is not null);
}

/// <summary>
/// What actually happened when a <see cref="SmartOptimizationPlan"/> was applied - separate counts
/// per category since Network alone can fail as a unit (elevation declined/failed) while
/// System/Gameplay/Graphics still succeed.
/// </summary>
public readonly record struct SmartOptimizationOutcome(
    int SystemChangesApplied,
    bool GameplayChangesApplied,
    bool GraphicsPresetApplied,
    int NetworkChangesRequested,
    bool NetworkChangesApplied,
    bool NetworkElevationCancelled);

/// <summary>
/// Builds and applies the app's one-click "Smart Optimization" action: every outstanding System/
/// Network/Gameplay tweak plus a recommended graphics preset, shared by the Dashboard's hero button
/// and the Optimizer page so both drive identical behavior.
/// </summary>
public interface ISmartOptimizationService
{
    /// <summary>
    /// Reads current System/Network/Gameplay/Graphics state and returns everything Smart
    /// Optimization would change. Read-only - nothing is written until <see cref="ApplyAsync"/> is
    /// called with the result the user confirmed.
    /// </summary>
    SmartOptimizationPlan BuildPlan();

    /// <summary>
    /// Applies every change in <paramref name="plan"/>: System tweaks directly, Network tweaks in
    /// one elevated batch (one UAC prompt), and - if included - the recommended graphics preset and
    /// outstanding Gameplay tweaks to client.cfg, behind a single backup covering both.
    /// </summary>
    Task<SmartOptimizationOutcome> ApplyAsync(SmartOptimizationPlan plan);
}