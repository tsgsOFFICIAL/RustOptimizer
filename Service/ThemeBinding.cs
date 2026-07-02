using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Service;

/// <summary>
/// Service for managing the application's theme.
/// </summary>
public static class ThemeBinding
{
    /// <summary>
    /// Gets the current theme variant of the application.
    /// </summary>
    /// <param name="target">The target element to bind the resource to.</param>
    /// <param name="property">The property to bind.</param>
    /// <param name="key">The key of the resource to bind.</param>
    public static void BindResource(this StyledElement target, AvaloniaProperty property, string key)
        => target.Bind(property, target.GetResourceObservable(key));
}