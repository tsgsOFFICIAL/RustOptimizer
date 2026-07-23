using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using RustOptimizer.Windows;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the About page: app identity, changelog/update-check dialogs, and outbound project links.
/// </summary>
public sealed class AboutViewModel : ViewModelBase
{
    private const string Unknown = "N/A";

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
        BuildText = GetBuildText();
        OsText = $"{Utility.GetFriendlyOsName()} ({Environment.OSVersion.Version})";

        // LicenseText resolves a localized string in C# rather than being a plain
        // {Binding Localization[Key]} lookup, so it has to be re-raised manually on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                OnPropertyChanged(nameof(LicenseText));
        };

        ViewChangelogCommand = new AsyncRelayCommand(ViewChangelogAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        OpenGitHubCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.GitHub));
        OpenDiscordCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.Discord));
        OpenKofiCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.KoFi));
    }

    /// <summary>The app's display version, e.g. "0.9.0".</summary>
    public string VersionText { get; }

    /// <summary>
    /// When this build was compiled, stamped into the assembly at build time. Two installs reporting
    /// the same version but different builds is exactly the kind of thing that explains a bug report.
    /// </summary>
    public string BuildText { get; }

    /// <summary>The host operating system and its build number.</summary>
    public string OsText { get; }

    /// <summary>
    /// The user's licence tier. Every install is currently Free - there is no paid tier - but the
    /// row exists so a future one has somewhere to appear, and so a support conversation can
    /// confirm which tier someone is on rather than assuming.
    /// </summary>
    public string LicenseText => Localization["LicenseFree"];

    /// <summary>
    /// Reads the build timestamp stamped into the assembly by the <c>BuildTimestamp</c> MSBuild
    /// property. Falls back to the executable's last-write time if the attribute is missing - that
    /// value can drift when the file is copied, so it's a fallback rather than the source of truth.
    /// </summary>
    private static string GetBuildText()
    {
        try
        {
            string? stamped = typeof(AboutViewModel).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "BuildTimestamp")?.Value;

            if (!string.IsNullOrWhiteSpace(stamped))
                return stamped;

            string exePath = Utility.GetExePath();
            return File.Exists(exePath)
                ? File.GetLastWriteTime(exePath).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : Unknown;
        }
        catch (Exception)
        {
            return Unknown;
        }
    }

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