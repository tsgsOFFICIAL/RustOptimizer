using RustOptimizer.Service.Logging;
using RustOptimizer.Interface;
using Avalonia.Styling;
using Avalonia;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Represents the available themes for the application.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly IAppSettingsService _settings;

    /// <summary>Creates the service. The theme value itself lives in <see cref="IAppSettingsService"/>.</summary>
    public ThemeService(IAppSettingsService settings) => _settings = settings;

    /// <summary>
    /// Gets the current theme of the application.
    /// </summary>
    public AppTheme Current { get; private set; } = AppTheme.System;

    /// <summary>
    /// Gets the actual theme variant of the application.
    /// </summary>
    public ThemeVariant ActualVariant => Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;

    /// <summary>
    /// Initializes the theme service by loading the saved theme preference and applying it.
    /// </summary>
    public void Initialize()
    {
        Current = Load();
        Apply(Current);
    }
    /// <summary>
    /// Sets the theme of the application and saves the preference.
    /// </summary>
    /// <param name="theme">The theme to set.</param>
    public void SetTheme(AppTheme theme)
    {
        Current = theme;
        Apply(theme);
        Save(theme);
    }
    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    public void ToggleLightDark()
    {
        SetTheme(ActualVariant == ThemeVariant.Dark ? AppTheme.Light : AppTheme.Dark);
    }
    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    private static void Apply(AppTheme theme)
    {
        if (Application.Current is not { } app)
            return;

        app.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
    /// <summary>
    /// Reads the saved theme from application settings.
    /// </summary>
    /// <returns>The loaded theme.</returns>
    private AppTheme Load() => _settings.Current.Theme;

    /// <summary>
    /// Writes the specified theme into application settings and persists them.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    private void Save(AppTheme theme)
    {
        try
        {
            _settings.Current.Theme = theme;
            _settings.Save();
        }
        catch (Exception ex)
        {
            AppLog.Warn("ThemeService", "Failed to save the theme preference.", ex);
        }
    }
}