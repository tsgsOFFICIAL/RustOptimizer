using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using System;

namespace RustOptimizer.Views;

/// <summary>
/// Theme and language switching, presented as two segmented controls (sun/moon/system for theme,
/// native language names for language) rather than the old plain toggle/EN-DK-RU buttons.
/// </summary>
public partial class SettingsView : UserControl
{
    private readonly IThemeService _theme;
    private readonly ILocalizationService _localization;

    private Button[] ThemeButtons => new[] { ThemeLightButton, ThemeDarkButton, ThemeSystemButton };
    private Button[] LanguageButtons => new[] { LanguageEnglishButton, LanguageDanishButton, LanguageRussianButton };

    public SettingsView() : this(CreateDesignTheme(), CreateDesignLocalization()) { }

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

    public SettingsView(IThemeService theme, ILocalizationService localization)
    {
        _theme = theme;
        _localization = localization;

        DataContext = localization;
        InitializeComponent();

        SetActive(ThemeButtons, _theme.Current.ToString());
        SetActive(LanguageButtons, _localization.Current.ToString());
    }

    private void OnThemeSegmentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } || !Enum.TryParse(tag, out AppTheme theme))
            return;

        _theme.SetTheme(theme);
        SetActive(ThemeButtons, tag);
    }

    private void OnLanguageSegmentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } || !Enum.TryParse(tag, out AppLanguage language))
            return;

        _localization.SetLanguage(language);
        SetActive(LanguageButtons, tag);
    }

    /// <summary>
    /// Highlights whichever button's tag matches the current selection, clearing the rest.
    /// </summary>
    private static void SetActive(Button[] buttons, string activeTag)
    {
        foreach (Button button in buttons)
            button.Classes.Set("active", (string)button.Tag! == activeTag);
    }
}