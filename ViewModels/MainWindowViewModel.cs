using RustOptimizer.Service.Logging;
using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using RustOptimizer.Controls;
using System.Threading.Tasks;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// The application shell: owns the sidebar, swaps <see cref="CurrentPage"/> in response to its
/// navigation requests, and checks for updates once the window has opened.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _theme;
    private readonly IAppSettingsService _settings;
    private readonly IUpdateService _updates;
    private readonly IDialogService _dialogs;
    private readonly ISystemInfoService _systemInfo;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly INetworkTweaksService _networkTweaks;
    private readonly IRustProcessService _rustProcess;
    private readonly IConfigService _configService;
    private readonly ICleanupService _cleanup;
    private readonly IConfigBackupService _configBackup;
    private readonly ISmartOptimizationService _smartOptimization;

    private DashboardViewModel? _dashboard;
    private OptimizerViewModel? _optimizer;
    private SystemViewModel? _system;
    private GraphicsViewModel? _graphics;
    private NetworkViewModel? _network;
    private GameplayViewModel? _gameplay;
    private SettingsViewModel? _settingsPage;
    private AboutViewModel? _about;
    private UtilitiesViewModel? _utilities;
    private BackupRestoreViewModel? _backupRestore;
    private ComingSoonViewModel? _comingSoon;

    private ViewModelBase? _currentPage;

    /// <summary>Creates the shell, its sidebar, and the initial Dashboard page.</summary>
    public MainWindowViewModel(IThemeService theme, ILocalizationService localization, IUpdateService updates,
        IRustProcessService rustProcess, ISystemInfoService systemInfo, ISystemTweaksService systemTweaks,
        INetworkTweaksService networkTweaks, IDialogService dialogs, IConfigService configService, IConfigBackupService configBackup,
        ICleanupService cleanup, ISmartOptimizationService smartOptimization, IAppSettingsService settings)
        : base(localization)
    {
        _theme = theme;
        _settings = settings;
        _updates = updates;
        _dialogs = dialogs;
        _systemInfo = systemInfo;
        _systemTweaks = systemTweaks;
        _networkTweaks = networkTweaks;
        _rustProcess = rustProcess;
        _configService = configService;
        _cleanup = cleanup;
        _configBackup = configBackup;
        _smartOptimization = smartOptimization;

        Sidebar = new SidebarViewModel(localization, rustProcess);
        Sidebar.NavigationRequested += (_, page) => Navigate(page);

        VersionText = Utility.GetDisplayVersion();
        OpenGitHubCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.GitHub));
        OpenDiscordCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.Discord));
        OpenKofiCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.KoFi));

        _dashboard = new DashboardViewModel(localization, systemInfo, systemTweaks, networkTweaks, rustProcess, configService, cleanup, dialogs, smartOptimization, settings, Sidebar);
        _dashboard.SystemDetailsRequested += (_, _) => Sidebar.NavigateTo(SidebarPage.System);
        _dashboard.NetworkDetailsRequested += (_, _) => Sidebar.NavigateTo(SidebarPage.Network);
        _dashboard.GameplayDetailsRequested += (_, _) => Sidebar.NavigateTo(SidebarPage.Gameplay);
        _dashboard.ManageProfilesRequested += (_, _) => Sidebar.NavigateTo(SidebarPage.Graphics);
        CurrentPage = _dashboard;
    }

    /// <summary>The nav rail's view model.</summary>
    public SidebarViewModel Sidebar { get; }

    /// <summary>The app's display version, shown in the sidebar footer.</summary>
    public string VersionText { get; }

    /// <summary>Opens the project's GitHub page.</summary>
    public RelayCommand OpenGitHubCommand { get; }

    /// <summary>Opens the project's Discord server.</summary>
    public RelayCommand OpenDiscordCommand { get; }

    /// <summary>Opens the project's Ko-fi page. Lives in the footer so it's reachable from any page.</summary>
    public RelayCommand OpenKofiCommand { get; }

    /// <summary>The view model of the page currently shown in the main content area.</summary>
    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    /// <summary>
    /// Checks GitHub for a newer release and, if one exists, prompts the user via
    /// <see cref="IDialogService"/>. Fails silently (e.g. offline) so it never blocks startup.
    /// Call once, after the window has opened.
    /// </summary>
    public async Task CheckForUpdatesOnStartupAsync()
    {
        if (!_settings.Current.CheckForUpdatesOnStartup)
            return;

        UpdateInfo? update;
        try
        {
            update = await _updates.CheckForUpdateAsync();
        }
        catch
        {
            return;
        }

        if (update is null)
            return;

        // Auto-update skips the prompt entirely: ApplyUpdateAsync swaps the install and terminates
        // this process, so there's nothing to show afterwards. Opt-in only - see AppSettings.AutoUpdate.
        if (_settings.Current.AutoUpdate)
        {
            try
            {
                await _updates.ApplyUpdateAsync(update);
            }
            catch (Exception ex)
            {
                AppLog.Warn("MainWindowViewModel", "Automatic update failed; falling back to the prompt.", ex);
            }
        }

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

    /// <summary>
    /// Swaps <see cref="CurrentPage"/> to match the sidebar selection. Every page has real content
    /// except whatever <see cref="ShowComingSoon"/> still covers.
    /// </summary>
    private void Navigate(SidebarPage page)
    {
        CurrentPage = page switch
        {
            SidebarPage.Dashboard => _dashboard ??= new DashboardViewModel(Localization, _systemInfo, _systemTweaks, _networkTweaks, _rustProcess, _configService, _cleanup, _dialogs, _smartOptimization, _settings, Sidebar),
            SidebarPage.Optimizer => _optimizer ??= new OptimizerViewModel(Localization, _smartOptimization, _dialogs, _systemTweaks, _networkTweaks, _configService, _systemInfo, _rustProcess, Sidebar),
            SidebarPage.System => _system ??= new SystemViewModel(Localization, _systemInfo, _systemTweaks, _rustProcess),
            SidebarPage.Graphics => _graphics ??= new GraphicsViewModel(Localization, _configService, _settings, _dialogs, Sidebar),
            SidebarPage.Network => _network ??= new NetworkViewModel(Localization, _networkTweaks, _settings, _dialogs),
            SidebarPage.Gameplay => _gameplay ??= new GameplayViewModel(Localization, _configService, Sidebar),
            // SettingsViewModel reads the Windows registry for the "start with Windows" toggle;
            // this app only ever runs on Windows (see app.manifest), same as Program.cs assumes.
#pragma warning disable CA1416
            SidebarPage.Settings => _settingsPage ??= new SettingsViewModel(_theme, Localization, _settings),
#pragma warning restore CA1416
            SidebarPage.About => _about ??= new AboutViewModel(Localization, _updates, _dialogs),
            SidebarPage.Utilities => _utilities ??= new UtilitiesViewModel(Localization),
            SidebarPage.BackupRestore => _backupRestore ??= new BackupRestoreViewModel(Localization, _configBackup, _rustProcess, Sidebar, _dialogs),
            _ => ShowComingSoon(page)
        };
    }

    /// <summary>Returns the shared "coming soon" placeholder, retitled for the given page.</summary>
    private ComingSoonViewModel ShowComingSoon(SidebarPage page)
    {
        ComingSoonViewModel comingSoon = _comingSoon ??= new ComingSoonViewModel(Localization);
        comingSoon.Title = Localization[$"Nav{page}"];
        return comingSoon;
    }
}