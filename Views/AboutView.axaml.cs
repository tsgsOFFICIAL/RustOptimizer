using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Windows;
using RustOptimizer.Service;
using Avalonia.Controls;
using System;

namespace RustOptimizer.Views;

/// <summary>
/// App identity, a manual update check, and links out to the project's GitHub/Discord/Ko-fi.
/// </summary>
public partial class AboutView : UserControl
{
    private readonly ILocalizationService _localization;
    private readonly IUpdateService _updates;

    public AboutView() : this(CreateDesignLocalization(), new UpdateService()) { }

    /// <summary>
    /// Creates an initialized localization service for the Avalonia previewer.
    /// </summary>
    private static LocalizationService CreateDesignLocalization()
    {
        LocalizationService localization = new();
        localization.Initialize();
        return localization;
    }

    public AboutView(ILocalizationService localization, IUpdateService updates)
    {
        _localization = localization;
        _updates = updates;

        DataContext = localization;
        InitializeComponent();

        VersionText.Text = Utility.GetDisplayVersion();
    }

    private async void OnViewChangelogClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        ChangelogWindow changelog = new(_localization, ChangelogWindow.LoadBundledChangelog(_localization.Current));
        await changelog.ShowDialog(owner);
    }

    private async void OnCheckForUpdatesClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        CheckForUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = _localization["CheckingForUpdates"];
        UpdateStatusText.IsVisible = true;

        UpdateInfo? update;
        try
        {
            update = await _updates.CheckForUpdateAsync();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"{_localization["UpdateFailed"]} {ex.Message}";
            CheckForUpdatesButton.IsEnabled = true;
            return;
        }

        if (update is null)
        {
            UpdateStatusText.Text = _localization["UpToDate"];
            CheckForUpdatesButton.IsEnabled = true;
            return;
        }

        UpdateStatusText.IsVisible = false;
        CheckForUpdatesButton.IsEnabled = true;

        string changes;
        try
        {
            changes = await RemoteChangelog.GetChangesSinceAsync(_localization.Current, Utility.GetDisplayVersion());
        }
        catch
        {
            changes = "";
        }

        UpdateAvailableWindow prompt = new(_localization, _updates, update, changes);
        await prompt.ShowDialog(owner);
    }

    private void OnGitHubClick(object? sender, RoutedEventArgs e) => Utility.OpenUrl(ProjectLinks.GitHub);

    private void OnDiscordClick(object? sender, RoutedEventArgs e) => Utility.OpenUrl(ProjectLinks.Discord);

    private void OnKofiClick(object? sender, RoutedEventArgs e) => Utility.OpenUrl(ProjectLinks.KoFi);
}