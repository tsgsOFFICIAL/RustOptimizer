using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia;

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
        }

        private void OnThemeToggle(object? sender, RoutedEventArgs e)
            => _theme.ToggleLightDark();

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