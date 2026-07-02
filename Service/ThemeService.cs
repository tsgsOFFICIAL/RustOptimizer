using RustOptimizer.Interface;
using Avalonia.Styling;
using System.IO;
using Avalonia;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Represents the available themes for the application.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private static readonly string PrefPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "theme.tsgs");

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
    /// Saves the specified theme preference to a file.
    /// </summary>
    /// <returns>The loaded theme.</returns>
    private static AppTheme Load()
    {
        try
        {
            if (!File.Exists(PrefPath))
                return AppTheme.System;

            return File.ReadAllText(PrefPath).Trim() switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.System
            };
        }
        catch
        {
            return AppTheme.System;
        }
    }
    /// <summary>
    /// Saves the specified theme preference to a file.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    private static void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);
            File.WriteAllText(PrefPath, theme.ToString());
        }
        catch
        { }
    }
}