using System.Threading.Tasks;

namespace RustOptimizer.Interface;

/// <summary>
/// Shows modal dialogs owned by the app's main window, keeping view models from constructing
/// <see cref="Avalonia.Controls.Window"/>s directly.
/// </summary>
public interface IDialogService
{
    /// <summary>Shows the changelog viewer with the given Markdown content.</summary>
    Task ShowChangelogAsync(ILocalizationService localization, string markdown);

    /// <summary>Shows the "update available" prompt for the given release.</summary>
    Task ShowUpdateAvailableAsync(ILocalizationService localization, IUpdateService updates, UpdateInfo update, string changelog);

    /// <summary>
    /// Shows a generic Yes/No confirmation prompt for an action that shouldn't happen from a
    /// single accidental click (e.g. restoring over or deleting a backup). Returns <see langword="true"/>
    /// only if the user picked the confirm option; closing the window any other way counts as "no".
    /// </summary>
    Task<bool> ShowConfirmAsync(ILocalizationService localization, string title, string message, string confirmLabel, bool isDestructive);

    /// <summary>
    /// Shows the Clear Cache prompt and keeps it open for the duration of the run, returning what
    /// the cleanup achieved or <see langword="null"/> if the user cancelled before it started. Every
    /// option starts enabled - the prompt exists so the few consequential targets (Recycle Bin,
    /// thumbnail cache, admin-only files) can be opted out of, not opted into.
    /// </summary>
    Task<CleanupOutcome?> ShowClearCacheAsync(ILocalizationService localization, ICleanupService cleanup);

    /// <summary>
    /// Shows a single-line text prompt (e.g. naming a graphics profile) and returns the entered text
    /// trimmed, or <see langword="null"/> if the user cancelled or left it blank. <paramref name="initialValue"/>
    /// pre-fills the field (e.g. the current name when renaming).
    /// </summary>
    Task<string?> ShowPromptAsync(ILocalizationService localization, string title, string message, string confirmLabel, string initialValue);

    /// <summary>
    /// Shows the "Update Drivers" prompt: what's detected for the CPU, GPU and motherboard, each with
    /// a link to its vendor's own driver page where one is known.
    /// </summary>
    Task ShowUpdateDriversAsync(ILocalizationService localization, ISystemInfoService systemInfo);

    /// <summary>
    /// Shows the Smart Optimization confirm prompt for an already-built <paramref name="plan"/> and,
    /// if confirmed, applies it itself before closing. Returns the applied outcome, or
    /// <see langword="null"/> if the user cancelled before anything ran.
    /// </summary>
    Task<SmartOptimizationOutcome?> ShowSmartOptimizationAsync(ILocalizationService localization, ISmartOptimizationService smartOptimization, SmartOptimizationPlan plan);
}