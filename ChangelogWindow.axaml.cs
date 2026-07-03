using RustOptimizer.Interface;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using System.Reflection;
using System.IO;

namespace RustOptimizer;

/// <summary>
/// A simple, standalone window that renders a block of Markdown (e.g. a CHANGELOG.md, or release
/// notes fetched by an update check) so the user can see *why* an update happened, not just that
/// a new version exists.
/// </summary>
public partial class ChangelogWindow : Window
{
    public ChangelogWindow() : this(CreateDesignLocalization(), "# Changelog\n\n- Sample entry") { }

    /// <summary>
    /// Creates an initialized localization service for the Avalonia previewer.
    /// </summary>
    private static LocalizationService CreateDesignLocalization()
    {
        LocalizationService localization = new();
        localization.Initialize();
        return localization;
    }

    /// <summary>
    /// Creates the changelog window and renders the given Markdown into it.
    /// </summary>
    /// <param name="localization">The localization service used to resolve UI strings.</param>
    /// <param name="markdown">The Markdown content to display (e.g. loaded from a CHANGELOG.md or fetched release notes).</param>
    public ChangelogWindow(ILocalizationService localization, string markdown)
    {
        DataContext = localization;
        InitializeComponent();
        ContentHost.Content = MarkdownRenderer.Render(markdown);
    }

    /// <summary>
    /// Loads the CHANGELOG.md bundled with the application as an embedded resource, merged with the
    /// matching locale file (e.g. CHANGELOG.da-DK.md) when one is embedded, falling back to English
    /// for any version the translation hasn't caught up on yet.
    /// </summary>
    /// <param name="language">The language to prefer for the changelog text.</param>
    /// <returns>The changelog contents, or an empty string if the English baseline could not be found.</returns>
    public static string LoadBundledChangelog(AppLanguage language)
    {
        string english = LoadEmbeddedResource("CHANGELOG.md");
        if (english.Length == 0 || language == AppLanguage.English)
            return english;

        string localized = LoadEmbeddedResource(LocaleFileName(language));
        return localized.Length == 0 ? english : ChangelogParser.MergeWithFallback(localized, english);
    }

    /// <summary>
    /// Reads an embedded text resource from this assembly by file name.
    /// </summary>
    /// <returns>The resource contents, or an empty string if it isn't embedded.</returns>
    private static string LoadEmbeddedResource(string fileName)
    {
        Assembly assembly = typeof(ChangelogWindow).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream($"RustOptimizer.{fileName}");
        if (stream is null)
            return string.Empty;

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Maps an application language to its changelog file name. Mirrors
    /// <c>LocalizationService</c>'s private locale-file-name mapping.
    /// </summary>
    private static string LocaleFileName(AppLanguage language) => language switch
    {
        AppLanguage.Danish => "CHANGELOG.da-DK.md",
        AppLanguage.Russian => "CHANGELOG.ru-RU.md",
        _ => "CHANGELOG.md"
    };

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}