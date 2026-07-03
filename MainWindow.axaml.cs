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

        public MainWindow() : this(CreateDesignTheme(), CreateDesignLocalization()) { }

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

        public MainWindow(IThemeService theme, ILocalizationService localization)
        {
            _theme = theme;
            _localization = localization;
            DataContext = localization;
            InitializeComponent();

            Opened += OnMainWindowOpened;
        }

        /// <summary>
        /// Checks for a newer changelog on GitHub once the window is shown, and displays the
        /// cumulative "what's new" view if the installed version is behind. Runs once per launch and
        /// fails silently (e.g. offline) so it never blocks or interrupts startup.
        /// </summary>
        private async void OnMainWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnMainWindowOpened;

            string changes;
            try
            {
                changes = await RemoteChangelog.GetChangesSinceAsync(_localization.Current, Utility.GetDisplayVersion());
            }
            catch
            {
                return;
            }

            if (changes.Length == 0)
                return;

            ChangelogWindow changelog = new(_localization, changes);
            await changelog.ShowDialog(this);
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