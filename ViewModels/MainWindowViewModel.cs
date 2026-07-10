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
    private readonly IRustProcessService _rustProcess;
    private readonly IConfigService _configService;

    private DashboardViewModel? _dashboard;
    private SettingsViewModel? _settings;
    private AboutViewModel? _about;
    private ComingSoonViewModel? _comingSoon;

    private ViewModelBase? _currentPage;

    public MainWindowViewModel(IThemeService theme, ILocalizationService localization, IUpdateService updates,
        IRustProcessService rustProcess, ISystemInfoService systemInfo, IDialogService dialogs, IConfigService configService)
        : base(localization)
    {
        _theme = theme;
        _updates = updates;
        _dialogs = dialogs;
        _systemInfo = systemInfo;
        _rustProcess = rustProcess;
        _configService = configService;

        Sidebar = new SidebarViewModel(localization, rustProcess);
        Sidebar.NavigationRequested += (_, page) => Navigate(page);

        VersionText = Utility.GetDisplayVersion();
        OpenGitHubCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.GitHub));
        OpenDiscordCommand = new RelayCommand(() => Utility.OpenUrl(ProjectLinks.Discord));

        CurrentPage = _dashboard = new DashboardViewModel(localization, systemInfo, rustProcess, configService);
    }

    public SidebarViewModel Sidebar { get; }

    public string VersionText { get; }

    public RelayCommand OpenGitHubCommand { get; }
    public RelayCommand OpenDiscordCommand { get; }

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
    /// Swaps <see cref="CurrentPage"/> to match the sidebar selection. Dashboard/Settings/About have
    /// real content; every other page is still a "coming soon" placeholder pending later phases.
    /// </summary>
    private void Navigate(SidebarPage page)
    {
        CurrentPage = page switch
        {
            SidebarPage.Dashboard => _dashboard ??= new DashboardViewModel(Localization, _systemInfo, _rustProcess, _configService),
            SidebarPage.Settings => _settings ??= new SettingsViewModel(_theme, Localization),
            SidebarPage.About => _about ??= new AboutViewModel(Localization, _updates, _dialogs),
            _ => ShowComingSoon(page)
        };
    }

    private ComingSoonViewModel ShowComingSoon(SidebarPage page)
    {
        ComingSoonViewModel comingSoon = _comingSoon ??= new ComingSoonViewModel(Localization);
        comingSoon.Title = Localization[$"Nav{page}"];
        return comingSoon;
    }
}