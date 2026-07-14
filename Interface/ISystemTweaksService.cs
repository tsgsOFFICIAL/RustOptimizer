using System.Collections.Generic;

namespace RustOptimizer.Interface;

/// <summary>One Windows power plan, as reported by <c>powercfg /list</c>.</summary>
public readonly record struct PowerPlanInfo(string Id, string Name, bool IsActive);

/// <summary>
/// The subset of gaming-relevant Windows settings this app can toggle without admin rights.
/// <see cref="FullscreenOptimizationsDisabledForRust"/> is <see langword="null"/> when Rust's
/// install path can't be resolved (not installed, or Steam itself can't be found) - there's
/// nothing to toggle it for in that case.
/// </summary>
public readonly record struct GamingTweaksSettings(
    bool PointerPrecisionEnabled,
    bool GameModeEnabled,
    bool BackgroundRecordingEnabled,
    bool? FullscreenOptimizationsDisabledForRust);

/// <summary>
/// Reads and applies OS-level tweaks that don't require administrator rights. Unlike
/// <see cref="ISystemInfoService"/> (read-only), this service actually changes system state.
/// </summary>
public interface ISystemTweaksService
{
    /// <summary>Gets every power plan Windows knows about, with the active one flagged.</summary>
    IReadOnlyList<PowerPlanInfo> GetPowerPlans();

    /// <summary>Activates the power plan with the given <see cref="PowerPlanInfo.Id"/>. Returns whether it succeeded.</summary>
    bool SetActivePowerPlan(string planId);

    /// <summary>Gets the current state of every gaming tweak this service can toggle.</summary>
    GamingTweaksSettings GetGamingTweaksSettings();

    /// <summary>Enables/disables mouse acceleration ("Enhance pointer precision"). Returns whether it succeeded.</summary>
    bool SetPointerPrecisionEnabled(bool enabled);

    /// <summary>Enables/disables Windows Game Mode. Returns whether it succeeded.</summary>
    bool SetGameModeEnabled(bool enabled);

    /// <summary>
    /// Enables/disables Xbox Game Bar's background game-clip recording - both the master switch
    /// ("Record in the background while I'm playing a game") and the separate instant-replay
    /// buffer ("Record what happened"), which lives under a different registry key. Returns
    /// whether it succeeded.
    /// </summary>
    bool SetBackgroundRecordingEnabled(bool enabled);

    /// <summary>
    /// Disables/re-enables Windows' fullscreen optimizations specifically for Rust's executable -
    /// the same mechanism as the "Disable fullscreen optimizations" checkbox on an exe's
    /// Properties &gt; Compatibility tab. Returns whether it succeeded; fails if Rust's install
    /// path can't be resolved.
    /// </summary>
    bool SetFullscreenOptimizationsDisabledForRust(bool disabled);
}