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
}