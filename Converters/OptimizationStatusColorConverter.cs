using RustOptimizer.ViewModels;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia;
using System;

namespace RustOptimizer.Converters;

/// <summary>
/// Converts an Optimization Overview tile's <see cref="OptimizationStatus"/> into its status
/// color - red/orange/green for not/partially/fully optimized - looked up as an application
/// resource so it follows theme changes. Reusable by any tile that follows the same convention,
/// e.g. <c>Foreground="{Binding SystemScore.Status, Converter={x:Static
/// converters:OptimizationStatusColorConverter.Instance}}"</c>.
/// </summary>
public sealed class OptimizationStatusColorConverter : IValueConverter
{
    public static readonly OptimizationStatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string key = value switch
        {
            OptimizationStatus.Optimized => "SuccessColor",
            OptimizationStatus.PartiallyOptimized => "AccentColor",
            _ => "DangerColor"
        };
        return Application.Current?.Resources.TryGetResource(key, null, out object? brush) == true ? brush : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}