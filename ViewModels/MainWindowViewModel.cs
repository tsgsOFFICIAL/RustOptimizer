using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using RustOptimizer.Controls;
using System.Threading.Tasks;

namespace RustOptimizer.ViewModels;

/// <summary>
/// The application shell: owns the sidebar, swaps <see cref="CurrentPage"/> in response to its
/// navigation requests, and checks for updates once the window has opened.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _theme;
    private readonly IUpdateService _updates;
    private readonly IDialogService _dialogs;
    private readonly ISystemInfoService _systemInfo;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly IRustProcessService _rustProcess;
    private readonly IConfigService _configService;
    private readonly IConfigBackupService _configBackup;

    private DashboardViewModel? _dashboard;
    private SystemViewModel? _system;
    private GraphicsViewModel? _graphics;
    private GameplayViewModel? _gameplay;
    private SettingsViewModel? _settings;
    private AboutViewModel? _about;
    private UtilitiesViewModel? _utilities;
    private BackupRestoreViewModel? _backupRestore;
    private ComingSoonViewModel? _comingSoon;

    private ViewModelBase? _currentPage;

    /// <summary>Creates the shell, its sidebar, and the initial Dashboard page.</summary>
    public MainWindowViewModel(IThemeService theme, ILocalizationService localization, IUpdateService updates,
        IRustProcessService rustProcess, ISystemInfoService systemInfo, ISystemTweaksService systemTweaks,
        IDialogService dialogs, IConfigService configService, IConfigBackupService configBackup)
        : base(localization)
    {
        _theme = theme;
        _updates = updates;
        _dialogs = dialogs;
        _systemInfo = systemInfo;
        _systemTweaks = systemTweaks;
        _rustProcess = rustProcess;
        _configService = configService;
        _configBackup = configBackup;

        Sidebar = new SidebarViewModel(localization, rustProcess);
        Sidebar.NavigationRequested += (_, page) => Navigate(page);

        VersionText = Utility.GetDisplayVersion();
        OpenGitHubCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.GitHub));
        OpenDiscordCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.Discord));

        _dashboard = new DashboardViewModel(localization, systemInfo, systemTweaks, rustProcess, configService, Sidebar);
        _dashboard.SystemDetailsRequested += (_, _) => Sidebar.NavigateTo(SidebarPage.System);
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
    /// Swaps <see cref="CurrentPage"/> to match the sidebar selection. Dashboard/System/Graphics/Gameplay/Settings/
    /// About/Utilities/BackupRestore have real content; every other page is still a "coming soon" placeholder pending later phases.
    /// </summary>
    private void Navigate(SidebarPage page)
    {
        CurrentPage = page switch
        {
            SidebarPage.Dashboard => _dashboard ??= new DashboardViewModel(Localization, _systemInfo, _systemTweaks, _rustProcess, _configService, Sidebar),
            SidebarPage.System => _system ??= new SystemViewModel(Localization, _systemInfo, _systemTweaks, _rustProcess),
            SidebarPage.Graphics => _graphics ??= new GraphicsViewModel(Localization, _configService, Sidebar),
            SidebarPage.Gameplay => _gameplay ??= new GameplayViewModel(Localization, _configService, Sidebar),
            SidebarPage.Settings => _settings ??= new SettingsViewModel(_theme, Localization),
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