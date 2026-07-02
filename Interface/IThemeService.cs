using Avalonia.Styling;

namespace RustOptimizer.Interface;

/// <summary>
/// Represents the available themes for the application.
/// </summary>
public enum AppTheme
{
    System,
    Light,
    Dark
}

/// <summary>
/// Service for managing the application's theme.
/// </summary>
public interface IThemeService
{
    AppTheme Current { get; }
    ThemeVariant ActualVariant { get; }
    void Initialize();
    void SetTheme(AppTheme theme);
    void ToggleLightDark();
}