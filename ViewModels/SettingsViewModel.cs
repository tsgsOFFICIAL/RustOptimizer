using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives theme and language switching, presented as two segmented controls.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _theme;

    private AppTheme _currentTheme;
    private AppLanguage _currentLanguage;

    public SettingsViewModel(IThemeService theme, ILocalizationService localization) : base(localization)
    {
        _theme = theme;
        _currentTheme = theme.Current;
        _currentLanguage = localization.Current;

        SetThemeCommand = new RelayCommand<string>(tag =>
        {
            if (Enum.TryParse(tag, out AppTheme value))
            {
                _theme.SetTheme(value);
                CurrentTheme = value;
            }
        });

        SetLanguageCommand = new RelayCommand<string>(tag =>
        {
            if (Enum.TryParse(tag, out AppLanguage value))
            {
                Localization.SetLanguage(value);
                CurrentLanguage = value;
            }
        });
    }

    public RelayCommand<string> SetThemeCommand { get; }
    public RelayCommand<string> SetLanguageCommand { get; }

    public AppTheme CurrentTheme { get => _currentTheme; private set => SetProperty(ref _currentTheme, value); }
    public AppLanguage CurrentLanguage { get => _currentLanguage; private set => SetProperty(ref _currentLanguage, value); }
}