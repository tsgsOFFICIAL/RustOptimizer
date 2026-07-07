using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Controls;
using RustOptimizer.Windows;
using RustOptimizer.Service;
using RustOptimizer.Views;
using Avalonia.Threading;
using Avalonia.Controls;
using System;

namespace RustOptimizer
{
    public partial class MainWindow : Window
    {
        private readonly IThemeService _theme;
        private readonly ILocalizationService _localization;
        private readonly IUpdateService _updates;
        private readonly IRustProcessService _rustProcess;

        private DashboardView? _dashboard;
        private SettingsView? _settings;
        private AboutView? _about;
        private ComingSoonView? _comingSoon;
        private DispatcherTimer? _rustPollTimer;

        public MainWindow()
            : this(CreateDesignTheme(), CreateDesignLocalization(), CreateDesignUpdates(), CreateDesignRustProcess()) { }

        /// <summary>
        /// Creates an initialized theme service for the Avalonia previewer.
        /// </summary>
        private static ThemeService CreateDesignTheme()
        {
            ThemeService theme = new();
            theme.Initialize();
            return theme;
        }

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
        /// Creates an update service for the Avalonia previewer. Never actually invoked there, since
        /// the update check only runs outside design mode.
        /// </summary>
        private static UpdateService CreateDesignUpdates() => new();

        /// <summary>
        /// Creates a Rust process service for the Avalonia previewer. Never actually invoked there,
        /// since the poll timer only runs outside design mode.
        /// </summary>
        private static RustProcessService CreateDesignRustProcess() => new();

        public MainWindow(IThemeService theme, ILocalizationService localization, IUpdateService updates,
            IRustProcessService rustProcess)
        {
            _theme = theme;
            _localization = localization;
            _updates = updates;
            _rustProcess = rustProcess;
            DataContext = localization;
            InitializeComponent();

            FooterVersionText.Text = Utility.GetDisplayVersion();
            MainContent.Content = _dashboard = new DashboardView();

            if (!Design.IsDesignMode)
            {
                Opened += OnMainWindowOpened;

                AppSidebar.SetRustRunning(_rustProcess.IsRunning());
                _rustPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _rustPollTimer.Tick += (_, _) => AppSidebar.SetRustRunning(_rustProcess.IsRunning());
                _rustPollTimer.Start();
                Closed += (_, _) => _rustPollTimer.Stop();
            }
        }

        /// <summary>
        /// Checks for a newer release on GitHub once the window is shown, and prompts the user with the
        /// available version (and a link to the changelog) if the installed version is behind. Shows
        /// nothing when already up to date. Runs once per launch and fails silently (e.g. offline) so it
        /// never blocks startup.
        /// </summary>
        private async void OnMainWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnMainWindowOpened;

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
                changes = await RemoteChangelog.GetChangesSinceAsync(_localization.Current, Utility.GetDisplayVersion());
            }
            catch
            {
                changes = "";
            }

            UpdateAvailableWindow prompt = new(_localization, _updates, update, changes);
            await prompt.ShowDialog(this);
        }

        /// <summary>
        /// Swaps the main content area to match the sidebar selection. Dashboard/Settings/About have
        /// real content; every other page is still a "coming soon" placeholder pending later phases.
        /// </summary>
        private void OnSidebarNavigationRequested(object? sender, SidebarPage page)
        {
            MainContent.Content = page switch
            {
                SidebarPage.Dashboard => _dashboard ??= new DashboardView(),
                SidebarPage.Settings => _settings ??= new SettingsView(_theme, _localization),
                SidebarPage.About => _about ??= new AboutView(_localization, _updates),
                _ => ShowComingSoon(page)
            };
        }

        private ComingSoonView ShowComingSoon(SidebarPage page)
        {
            ComingSoonView comingSoon = _comingSoon ??= new ComingSoonView();
            comingSoon.SetTitle(_localization[$"Nav{page}"]);
            return comingSoon;
        }

        private void OnLaunchRustRequested(object? sender, EventArgs e) => _rustProcess.Launch();

        private void OnGitHubClick(object? sender, RoutedEventArgs e) => Utility.OpenUrl(ProjectLinks.GitHub);

        private void OnDiscordClick(object? sender, RoutedEventArgs e) => Utility.OpenUrl(ProjectLinks.Discord);
    }
}