using IconPacks.Avalonia.PhosphorIcons;
using RustOptimizer.Service.Logging;
using RustOptimizer.Interface;
using Avalonia.Svg.Skia;
using Avalonia.Platform;
using Avalonia.Media;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One entry in the Settings page's language dropdown. Names are the language's own endonym
/// ("Dansk", not "Danish"), so the list reads correctly whichever language is currently active and
/// needs no translation of its own.
/// </summary>
/// <param name="Language">The language this row selects.</param>
/// <param name="Name">The language's name, written in that language.</param>
/// <param name="Flag">The flag image shown beside the name, or <see langword="null"/> if it couldn't load.</param>
public sealed record LanguageOption(AppLanguage Language, string Name, IImage? Flag)
{
    /// <summary>
    /// Builds an option, resolving its flag from the bundled lipis/flag-icons set by ISO 3166-1
    /// alpha-2 country code. The whole set ships, so a new language needs only its code here - no
    /// new asset. Loaded as an <see cref="IImage"/> rather than bound as a path string, because
    /// Avalonia won't convert a string to an image source through a binding.
    /// </summary>
    /// <param name="language">The language this option selects.</param>
    /// <param name="name">The language's name, written in that language.</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code naming the flag file, e.g. "dk".</param>
    public static LanguageOption Create(AppLanguage language, string name, string countryCode)
        => new(language, name, LoadFlag(countryCode));

    /// <summary>Loads a flag, returning <see langword="null"/> rather than throwing if it's missing or unparseable.</summary>
    private static IImage? LoadFlag(string countryCode)
    {
        try
        {
            Uri uri = new($"avares://RustOptimizer/Assets/Flags/{countryCode}.svg");
            if (!AssetLoader.Exists(uri))
            {
                AppLog.Warn("LanguageOption", $"Flag asset for '{countryCode}' is missing.");
                return null;
            }

            return new SvgImage { Source = SvgSource.Load(uri.ToString(), null) };
        }
        catch (Exception ex)
        {
            AppLog.Warn("LanguageOption", $"Failed to load the flag for '{countryCode}'.", ex);
            return null;
        }
    }
}

/// <summary>
/// One entry in the Settings page's theme dropdown. Unlike languages, the labels are translated,
/// so the list is rebuilt whenever the active language changes.
/// </summary>
/// <param name="Theme">The theme this row selects.</param>
/// <param name="Name">The theme's localized display name.</param>
/// <param name="Icon">The glyph shown beside the name - a sun, moon, or monitor.</param>
public sealed record ThemeOption(AppTheme Theme, string Name, PackIconPhosphorIconsKind Icon);