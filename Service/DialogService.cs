using Avalonia.Controls.ApplicationLifetimes;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using RustOptimizer.Windows;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IDialogService" />
public sealed class DialogService : IDialogService
{
    public async Task ShowChangelogAsync(ILocalizationService localization, string markdown)
    {
        if (GetOwner() is not { } owner)
            return;

        ChangelogWindow window = new(localization, markdown);
        await window.ShowDialog(owner);
    }

    public async Task ShowUpdateAvailableAsync(ILocalizationService localization, IUpdateService updates, UpdateInfo update, string changelog)
    {
        if (GetOwner() is not { } owner)
            return;

        UpdateAvailableWindow window = new(localization, updates, update, changelog);
        await window.ShowDialog(owner);
    }

    public async Task<bool> ShowConfirmAsync(ILocalizationService localization, string title, string message, string confirmLabel, bool isDestructive)
    {
        if (GetOwner() is not { } owner)
            return false;

        ConfirmDialogViewModel viewModel = new(localization, title, message, confirmLabel, isDestructive);
        ConfirmDialogWindow window = new(viewModel);
        return await window.ShowDialog<bool>(owner);
    }

    public async Task<CleanupOutcome?> ShowClearCacheAsync(ILocalizationService localization, ICleanupService cleanup)
    {
        if (GetOwner() is not { } owner)
            return null;

        ClearCacheDialogViewModel viewModel = new(localization, cleanup);
        ClearCacheDialogWindow window = new(viewModel);
        return await window.ShowDialog<CleanupOutcome?>(owner);
    }

    private static Window? GetOwner()
        => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}