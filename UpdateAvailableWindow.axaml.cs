using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using System;

namespace RustOptimizer;

/// <summary>
/// Prompts the user that a newer version is available, with the option to view what changed before
/// deciding, or to update immediately.
/// </summary>
public partial class UpdateAvailableWindow : Window
{
    private readonly ILocalizationService _localization;
    private readonly IUpdateService _updates;
    private readonly UpdateInfo _update;
    private readonly string _changelog;

    public UpdateAvailableWindow() : this(
        CreateDesignLocalization(),
        new UpdateService(),
        new UpdateInfo("0.0.0", "https://example.com", "RustOptimizer-0.0.0-self-contained.zip"),
        "# Sample for previewer")
    { }

    /// <summary>
    /// Creates an initialized localization service for the Avalonia previewer.
    /// </summary>
    private static LocalizationService CreateDesignLocalization()
    {
        LocalizationService localization = new();
        localization.Initialize();
        return localization;
    }

    /// <summary>
    /// Creates the update prompt for the given available version.
    /// </summary>
    /// <param name="localization">The localization service used to resolve UI strings.</param>
    /// <param name="updates">The update service used to apply the update if the user confirms.</param>
    /// <param name="update">The available update to offer.</param>
    /// <param name="changelog">The changes between the installed and available version, or empty if unavailable.</param>
    public UpdateAvailableWindow(ILocalizationService localization, IUpdateService updates, UpdateInfo update, string changelog)
    {
        _localization = localization;
        _updates = updates;
        _update = update;
        _changelog = changelog;

        DataContext = localization;
        InitializeComponent();

        VersionText.Text = update.Version;
        ChangelogButton.IsEnabled = changelog.Length > 0;
    }

    private async void OnViewChangelogClick(object? sender, RoutedEventArgs e)
    {
        ChangelogWindow changelog = new(_localization, _changelog);
        await changelog.ShowDialog(this);
    }

    private void OnLaterClick(object? sender, RoutedEventArgs e) => Close();

    private async void OnUpdateNowClick(object? sender, RoutedEventArgs e)
    {
        ChangelogButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        StatusText.Text = _localization["Updating"];
        StatusText.IsVisible = true;

        try
        {
            // On success this exits the process directly, so nothing after this call runs on the happy path.
            await _updates.ApplyUpdateAsync(_update);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{_localization["UpdateFailed"]} {ex.Message}";
            ChangelogButton.IsEnabled = _changelog.Length > 0;
            LaterButton.IsEnabled = true;
            UpdateButton.IsEnabled = true;
        }
    }
}
