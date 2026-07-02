using System.ComponentModel;

namespace RustOptimizer.Interface;

/// <summary>
/// Represents the available languages for the application.
/// </summary>
public enum AppLanguage
{
    English,
    Danish,
    Russian
}

/// <summary>
/// Defines an interface for a localization service that manages language settings and provides localized strings.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the current language of the application.
    /// </summary>
    AppLanguage Current { get; }

    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key defined in the locale JSON files.</param>
    string this[string key] { get; }

    /// <summary>
    /// Initializes the localization service by loading locale catalogs and applying the saved or detected language.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Sets the current language of the application and optionally saves the preference.
    /// </summary>
    /// <param name="language">The language to set.</param>
    /// <param name="save">Whether to persist the language preference to disk.</param>
    void SetLanguage(AppLanguage language, bool save = true);
}