using Avalonia.Data.Converters;
using System.Globalization;
using System;

namespace RustOptimizer.Converters;

/// <summary>
/// Compares any bound value against a string <c>ConverterParameter</c> by its text form, for the
/// same "active"-style segmented buttons <see cref="EnumEqualsConverter"/> handles - but for values
/// that aren't enums. <see cref="EnumEqualsConverter"/> calls <see cref="Enum.TryParse(Type, string, out object)"/>
/// on the bound value's own type, which throws outright for an <see cref="int"/>, so numeric
/// selections (e.g. a log retention of 7/30/90 days) need this instead.
/// </summary>
public sealed class ValueEqualsConverter : IValueConverter
{
    /// <summary>The shared instance, referenced from XAML via <c>{x:Static}</c>.</summary>
    public static readonly ValueEqualsConverter Instance = new();

    // Returning a bool from a method typed object? boxes it on every call, and these converters run
    // on every binding re-evaluation - once per button, per selection change. Two pre-boxed values
    // cost nothing and make the conversion allocation-free.
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    /// <summary>Returns whether the bound value's invariant string form equals the parameter.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool matches = value != null && parameter is string tag
            && string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture), tag, StringComparison.Ordinal);

        return matches ? BoxedTrue : BoxedFalse;
    }

    /// <summary>Not supported - these buttons write their value through a command, not a binding.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}