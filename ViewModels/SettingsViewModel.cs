using IconPacks.Avalonia.PhosphorIcons;
using RustOptimizer.Service.Logging;
using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Settings page: appearance, application behaviour, updates, display units and log
/// handling. Every value except <see cref="StartWithWindows"/> lives in <see cref="IAppSettingsService"/>
/// and is written straight back on change - there's no Apply button, so each toggle persists itself.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _theme;
    private readonly IAppSettingsService _settings;

    private AppTheme _currentTheme;
    private AppLanguage _currentLanguage;
    private bool _startWithWindows;
    private bool _checkForUpdatesOnStartup;
    private bool _autoUpdate;
    private ThroughputUnit _throughputUnit;
    private int _logRetentionDays;
    private bool _verboseLogging;
    private IReadOnlyList<ThemeOption> _themeOptions = [];
    private LanguageOption? _selectedLanguage;
    private ThemeOption? _selectedTheme;

    /// <summary>Creates the view model, seeding every control from the persisted settings.</summary>
    public SettingsViewModel(IThemeService theme, ILocalizationService localization, IAppSettingsService settings)
        : base(localization)
    {
        _theme = theme;
        _settings = settings;

        _currentTheme = theme.Current;
        _currentLanguage = localization.Current;
        _checkForUpdatesOnStartup = settings.Current.CheckForUpdatesOnStartup;
        _autoUpdate = settings.Current.AutoUpdate;
        _throughputUnit = settings.Current.ThroughputUnit;
        _logRetentionDays = settings.Current.LogRetentionDays;
        _verboseLogging = settings.Current.VerboseLogging;

        // Read from the registry rather than the saved setting, so an entry removed by another tool
        // (or a cleanup utility) shows the truth instead of a stale "on".
        _startWithWindows = StartupRegistration.IsRegistered();

        LanguageOptions =
        [
            LanguageOption.Create(AppLanguage.English, "English", "gb"),
            LanguageOption.Create(AppLanguage.Danish, "Dansk", "dk"),
            LanguageOption.Create(AppLanguage.Russian, "Русский", "ru")
        ];

        // Backing field, not the property: assigning through the setter here would re-apply the
        // language the app already has, during construction.
        foreach (LanguageOption option in LanguageOptions)
        {
            if (option.Language == _currentLanguage)
                _selectedLanguage = option;
        }

        RefreshThemeOptions();

        // Theme names are translated, so the dropdown's contents (and the selected item's label)
        // have to be rebuilt whenever the language changes.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                RefreshThemeOptions();
        };

        SetThroughputUnitCommand = new RelayCommand<string>(tag =>
        {
            if (Enum.TryParse(tag, out ThroughputUnit value))
                ThroughputUnit = value;
        });

        SetLogRetentionCommand = new RelayCommand<string>(tag =>
        {
            if (int.TryParse(tag, out int days))
                LogRetentionDays = days;
        });

        OpenLogFolderCommand = new RelayCommand(AppLog.OpenLogDirectory);
    }

    /// <summary>The selectable languages, each with its flag. Bound as the language dropdown's items.</summary>
    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    /// <summary>The selectable themes, with localized names. Rebuilt on every language change.</summary>
    public IReadOnlyList<ThemeOption> ThemeOptions
    {
        get => _themeOptions;
        private set => SetProperty(ref _themeOptions, value);
    }

    /// <summary>
    /// The language dropdown's selection. Setting it switches the app's language immediately -
    /// there's no Apply step, matching how every other setting on this page behaves.
    /// </summary>
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is null || !SetProperty(ref _selectedLanguage, value))
                return;

            Localization.SetLanguage(value.Language);
            CurrentLanguage = value.Language;
        }
    }

    /// <summary>The theme dropdown's selection. Setting it applies the theme immediately.</summary>
    public ThemeOption? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (value is null || !SetProperty(ref _selectedTheme, value))
                return;

            _theme.SetTheme(value.Theme);
            CurrentTheme = value.Theme;
        }
    }

    /// <summary>Applies the throughput display unit named by its parameter.</summary>
    public RelayCommand<string> SetThroughputUnitCommand { get; }

    /// <summary>Applies the log retention window, in days, given as its parameter.</summary>
    public RelayCommand<string> SetLogRetentionCommand { get; }

    /// <summary>Opens the folder holding the app's log files.</summary>
    public RelayCommand OpenLogFolderCommand { get; }

    /// <summary>The selected colour theme.</summary>
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        private set => SetProperty(ref _currentTheme, value);
    }

    /// <summary>The selected interface language.</summary>
    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        private set => SetProperty(ref _currentLanguage, value);
    }

    /// <summary>
    /// Whether the app launches with Windows. Setting this writes the registry entry first and only
    /// keeps the new value if that succeeded, so a failed write can't leave the toggle lying.
    /// </summary>
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
                return;

            if (!StartupRegistration.SetRegistered(value))
            {
                // Re-raise so the toggle snaps back to where it actually is.
                OnPropertyChanged(nameof(StartWithWindows));
                return;
            }

            _startWithWindows = value;
            _settings.Current.StartWithWindows = value;
            _settings.Save();
            OnPropertyChanged(nameof(StartWithWindows));
        }
    }

    /// <summary>Whether the app looks for a newer release each time it opens.</summary>
    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set
        {
            if (!SetProperty(ref _checkForUpdatesOnStartup, value))
                return;

            _settings.Current.CheckForUpdatesOnStartup = value;
            _settings.Save();
            OnPropertyChanged(nameof(CanAutoUpdate));
        }
    }

    /// <summary>Whether an available update is downloaded and applied without asking.</summary>
    public bool AutoUpdate
    {
        get => _autoUpdate;
        set
        {
            if (!SetProperty(ref _autoUpdate, value))
                return;

            _settings.Current.AutoUpdate = value;
            _settings.Save();
        }
    }

    /// <summary>
    /// Whether the auto-update toggle is usable. Automatic updates can only happen off the back of
    /// the startup check, so the row greys out when that's switched off rather than silently doing nothing.
    /// </summary>
    public bool CanAutoUpdate => CheckForUpdatesOnStartup;

    /// <summary>The unit network throughput is displayed in.</summary>
    public ThroughputUnit ThroughputUnit
    {
        get => _throughputUnit;
        private set
        {
            if (!SetProperty(ref _throughputUnit, value))
                return;

            _settings.Current.ThroughputUnit = value;
            _settings.Save();
        }
    }

    /// <summary>How many days of log files are kept.</summary>
    public int LogRetentionDays
    {
        get => _logRetentionDays;
        private set
        {
            if (!SetProperty(ref _logRetentionDays, value))
                return;

            _settings.Current.LogRetentionDays = value;
            _settings.Save();

            // Applied immediately rather than at next launch, so choosing a shorter window visibly
            // does something instead of appearing to be ignored.
            AppLog.ApplyRetention(value);
        }
    }

    /// <summary>
    /// Whether every log level is written. Applied immediately as well as saved, so a support
    /// session can turn it on and reproduce the problem without restarting the app first.
    /// </summary>
    public bool VerboseLogging
    {
        get => _verboseLogging;
        set
        {
            if (!SetProperty(ref _verboseLogging, value))
                return;

            _settings.Current.VerboseLogging = value;
            _settings.Save();
            AppLog.ApplyVerbose(value);
        }
    }

    /// <summary>The retention choices offered, in days. Used to render the segmented control.</summary>
    public IReadOnlyList<int> LogRetentionChoices { get; } = [7, 30, 90];

    /// <summary>
    /// Rebuilds the theme dropdown with names in the current language, preserving the selection.
    /// Assigning <see cref="_selectedTheme"/> through its backing field rather than the property
    /// avoids re-applying the theme as a side effect of merely relabelling it.
    /// </summary>
    private void RefreshThemeOptions()
    {
        ThemeOptions =
        [
            new ThemeOption(AppTheme.Light, Localization["ThemeLight"], PackIconPhosphorIconsKind.Sun),
            new ThemeOption(AppTheme.Dark, Localization["ThemeDark"], PackIconPhosphorIconsKind.Moon),
            new ThemeOption(AppTheme.System, Localization["ThemeSystem"], PackIconPhosphorIconsKind.Monitor)
        ];

        _selectedTheme = null;
        foreach (ThemeOption option in ThemeOptions)
        {
            if (option.Theme == CurrentTheme)
                _selectedTheme = option;
        }

        OnPropertyChanged(nameof(SelectedTheme));
    }
}
