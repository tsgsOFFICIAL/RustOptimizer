using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using RustOptimizer.Windows;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the About page: app identity, changelog/update-check dialogs, and outbound project links.
/// </summary>
public sealed class AboutViewModel : ViewModelBase
{
    private readonly IUpdateService _updates;
    private readonly IDialogService _dialogs;

    private string _updateStatusText = "";
    private bool _isUpdateStatusVisible;
    private bool _isCheckingForUpdates;

    public AboutViewModel(ILocalizationService localization, IUpdateService updates, IDialogService dialogs)
        : base(localization)
    {
        _updates = updates;
        _dialogs = dialogs;

        VersionText = Utility.GetDisplayVersion();

        ViewChangelogCommand = new AsyncRelayCommand(ViewChangelogAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        OpenGitHubCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.GitHub));
        OpenDiscordCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.Discord));
        OpenKofiCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.KoFi));
    }

    public string VersionText { get; }

    public AsyncRelayCommand ViewChangelogCommand { get; }
    public AsyncRelayCommand CheckForUpdatesCommand { get; }
    public RelayCommand OpenGitHubCommand { get; }
    public RelayCommand OpenDiscordCommand { get; }
    public RelayCommand OpenKofiCommand { get; }

    public string UpdateStatusText { get => _updateStatusText; private set => SetProperty(ref _updateStatusText, value); }
    public bool IsUpdateStatusVisible { get => _isUpdateStatusVisible; private set => SetProperty(ref _isUpdateStatusVisible, value); }
    public bool IsCheckingForUpdates { get => _isCheckingForUpdates; private set => SetProperty(ref _isCheckingForUpdates, value); }

    private Task ViewChangelogAsync()
        => _dialogs.ShowChangelogAsync(Localization, ChangelogWindow.LoadBundledChangelog(Localization.Current));

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatusText = Localization["CheckingForUpdates"];
        IsUpdateStatusVisible = true;

        UpdateInfo? update;
        try
        {
            update = await _updates.CheckForUpdateAsync();
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"{Localization["UpdateFailed"]} {ex.Message}";
            IsCheckingForUpdates = false;
            return;
        }

        if (update is null)
        {
            UpdateStatusText = Localization["UpToDate"];
            IsCheckingForUpdates = false;
            return;
        }

        // The check itself is done at this point - only the (optional) dialog is left, so
        // re-enable the button now rather than making the user wait on that too.
        IsUpdateStatusVisible = false;
        IsCheckingForUpdates = false;

        string changes;
        try
        {
            changes = await RemoteChangelog.GetChangesSinceAsync(Localization.Current, Utility.GetDisplayVersion());
        }
        catch
        {
            changes = "";
        }

        await _dialogs.ShowUpdateAvailableAsync(Localization, _updates, update, changes);
    }
}