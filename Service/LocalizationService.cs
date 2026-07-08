using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Represents a service for managing localization and language settings in the application.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly string PrefPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "language.tsgs");

    private const string IndexerName = "Item";
    private const string IndexerArrayName = "Item[]";

    private bool _initialized;

    private readonly Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> _catalogs = new();
    private IReadOnlyDictionary<string, string> _current = Empty();
    private IReadOnlyDictionary<string, string> _fallback = Empty();

    /// <summary>
    /// Gets the current language of the application.
    /// </summary>
    public AppLanguage Current { get; private set; }

    /// <summary>
    /// Occurs when a localized property changes, such as after a language switch.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the localized string for the specified key.
    /// Falls back to English, then to the key name itself when no translation exists.
    /// </summary>
    /// <param name="key">The resource key defined in the locale JSON files.</param>
    public string this[string key]
    {
        get
        {
            if (_current.TryGetValue(key, out string? value))
                return value;

            if (_fallback.TryGetValue(key, out string? fallback))
                return fallback;

            return key;
        }
    }

    /// <summary>
    /// Initializes the localization service by loading locale catalogs and applying the saved or detected language.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        LoadCatalogs();
        _fallback = Catalog(AppLanguage.English);
        Current = Load();
        Apply(Current);
        Notify();
    }

    /// <summary>
    /// Sets the current language of the application and optionally saves the preference.
    /// </summary>
    /// <param name="language">The language to set.</param>
    /// <param name="save">Whether to persist the language preference to disk.</param>
    public void SetLanguage(AppLanguage language, bool save = true)
    {
        Initialize();

        Current = language;
        Apply(language);

        if (save)
            Save(language);

        Notify();
    }

    /// <summary>
    /// Applies the specified language by selecting its string catalog and updating thread culture settings.
    /// </summary>
    /// <param name="language">The language to apply.</param>
    private void Apply(AppLanguage language)
    {
        // UI strings are resolved from the embedded JSON catalog, not from thread culture.
        _current = Catalog(language);

        CultureInfo culture = language switch
        {
            AppLanguage.Danish => new CultureInfo("da-DK"),
            AppLanguage.Russian => new CultureInfo("ru-RU"),
            _ => new CultureInfo("en")
        };

        // Set the thread culture to ensure proper formatting of dates, numbers, etc.
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    /// <summary>
    /// Loads all embedded locale catalogs into memory.
    /// </summary>
    private void LoadCatalogs()
    {
        foreach (AppLanguage language in Enum.GetValues<AppLanguage>())
            _catalogs[language] = LoadCatalog(language);
    }

    /// <summary>
    /// Gets the string catalog for the specified language.
    /// </summary>
    /// <param name="language">The language whose catalog should be returned.</param>
    /// <returns>The catalog for the language, or an empty catalog when unavailable.</returns>
    private IReadOnlyDictionary<string, string> Catalog(AppLanguage language)
        => _catalogs.TryGetValue(language, out IReadOnlyDictionary<string, string>? catalog)
            ? catalog
            : Empty();

    /// <summary>
    /// Loads a locale catalog from an embedded JSON resource.
    /// </summary>
    /// <param name="language">The language whose catalog should be loaded.</param>
    /// <returns>The loaded catalog, or an empty catalog when the resource is missing or invalid.</returns>
    private static IReadOnlyDictionary<string, string> LoadCatalog(AppLanguage language)
    {
        string resourceName = $"RustOptimizer.Lang.{ToFileName(language)}.json";
        Assembly assembly = typeof(LocalizationService).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return Empty();

        using StreamReader reader = new(stream);
        Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
        return data ?? Empty();
    }

    /// <summary>
    /// Maps an application language to its locale JSON file name.
    /// </summary>
    /// <param name="language">The language to map.</param>
    /// <returns>The locale file name without extension.</returns>
    private static string ToFileName(AppLanguage language) => language switch
    {
        AppLanguage.Danish => "da-DK",
        AppLanguage.Russian => "ru-RU",
        _ => "en"
    };

    /// <summary>
    /// Loads the saved language preference from the preference file.
    /// </summary>
    /// <returns>The loaded language, or a detected system language when no preference exists.</returns>
    private AppLanguage Load()
    {
        try
        {
            if (!File.Exists(PrefPath))
                return DetectSystemLanguage();

            return File.ReadAllText(PrefPath).Trim() switch
            {
                "Danish" => AppLanguage.Danish,
                "Russian" => AppLanguage.Russian,
                _ => AppLanguage.English
            };
        }
        catch (Exception ex)
        {
            AppLog.Warn("LocalizationService", $"Failed to load language preference from '{PrefPath}'.", ex);
            return AppLanguage.English;
        }
    }

    /// <summary>
    /// Detects the best matching application language from the operating system UI culture.
    /// </summary>
    /// <returns>The detected language, or <see cref="AppLanguage.English"/> when no match is found.</returns>
    private static AppLanguage DetectSystemLanguage()
    {
        string name = CultureInfo.CurrentUICulture.Name;

        if (name.StartsWith("da", StringComparison.OrdinalIgnoreCase))
            return AppLanguage.Danish;

        if (name.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return AppLanguage.Russian;

        return AppLanguage.English;
    }

    /// <summary>
    /// Saves the specified language preference to the preference file.
    /// </summary>
    /// <param name="language">The language to save.</param>
    private static void Save(AppLanguage language)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);
            File.WriteAllText(PrefPath, language.ToString());
        }
        catch (Exception ex)
        {
            AppLog.Warn("LocalizationService", $"Failed to save language preference to '{PrefPath}'.", ex);
        }
    }

    /// <summary>
    /// Notifies subscribers that localized bindings should refresh.
    /// </summary>
    private void Notify()
    {
        // Avalonia listens for "Item" (not just "Item[]") to refresh {Binding [Key]} paths.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerArrayName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }

    /// <summary>
    /// Creates an empty string catalog.
    /// </summary>
    /// <returns>An empty read-only dictionary.</returns>
    private static IReadOnlyDictionary<string, string> Empty()
        => new Dictionary<string, string>();
}