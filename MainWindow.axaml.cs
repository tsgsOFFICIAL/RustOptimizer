using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia;
using System;

namespace RustOptimizer
{
    public partial class MainWindow : Window
    {
        private readonly IThemeService _theme;
        private readonly ILocalizationService _localization;
        private readonly IUpdateService _updates;

        public MainWindow() : this(CreateDesignTheme(), CreateDesignLocalization(), CreateDesignUpdates()) { }

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

        public MainWindow(IThemeService theme, ILocalizationService localization, IUpdateService updates)
        {
            _theme = theme;
            _localization = localization;
            _updates = updates;
            DataContext = localization;
            InitializeComponent();

            if (!Design.IsDesignMode)
                Opened += OnMainWindowOpened;
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

        private void OnThemeToggle(object? sender, RoutedEventArgs e)
            => _theme.ToggleLightDark();

        private async void OnViewChangelog(object? sender, RoutedEventArgs e)
        {
            ChangelogWindow changelog = new(_localization, ChangelogWindow.LoadBundledChangelog(_localization.Current));
            await changelog.ShowDialog(this);
        }

        private void OnEnglish(object? sender, RoutedEventArgs e)
            => SetLanguage(AppLanguage.English);

        private void OnDanish(object? sender, RoutedEventArgs e)
            => SetLanguage(AppLanguage.Danish);

        private void OnRussian(object? sender, RoutedEventArgs e)
            => SetLanguage(AppLanguage.Russian);

        /// <summary>
        /// Sets the application language and skips persistence while running in the Avalonia previewer.
        /// </summary>
        /// <param name="language">The language to set.</param>
        private void SetLanguage(AppLanguage language)
            => _localization.SetLanguage(language, save: !Design.IsDesignMode);
    }
}