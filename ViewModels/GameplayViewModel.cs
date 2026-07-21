using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Gameplay page: a flat list of optional Rust client.cfg tweaks with no performance
/// cost (see <see cref="RustOptimizer.Service.RecommendedGameplayTweaks"/>), each independently
/// toggleable.
/// </summary>
public sealed class GameplayViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly SidebarViewModel _sidebar;
    private IReadOnlyList<GameplayTweakRow> _recommendedTweaks = [];
    private IReadOnlyList<GameplayTweakRow> _preferenceTweaks = [];
    private bool _recommendedAllEnabled;
    private bool _preferenceAllEnabled;

    /// <summary>Creates the view model and loads every tweak's current state.</summary>
    public GameplayViewModel(ILocalizationService localization, IConfigService configService, SidebarViewModel sidebar)
        : base(localization)
    {
        _configService = configService;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        RefreshTweaks();

        // Label/Description are resolved from RecommendedGameplayTweaks' localization keys up
        // front, so the whole row list needs rebuilding (not just re-binding) on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                RefreshTweaks();
        };
    }

    /// <summary>Tweaks with no real downside for anyone, regardless of taste or hardware.</summary>
    public IReadOnlyList<GameplayTweakRow> RecommendedTweaks
    {
        get => _recommendedTweaks;
        private set => SetProperty(ref _recommendedTweaks, value);
    }

    /// <summary>Tweaks that trade something (info density, personal feel, a little performance) reasonable players could disagree on.</summary>
    public IReadOnlyList<GameplayTweakRow> PreferenceTweaks
    {
        get => _preferenceTweaks;
        private set => SetProperty(ref _preferenceTweaks, value);
    }

    /// <summary>Whether toggles can be interacted with - client.cfg can't be safely written while Rust has it open.</summary>
    public bool CanToggleTweaks => !_sidebar.IsRustRunning;

    /// <summary>Whether every "Recommended for Everyone" tweak is currently enabled. Setting it enables/disables all of them together.</summary>
    public bool RecommendedAllEnabled
    {
        get => _recommendedAllEnabled;
        set => SetCategoryToggle(GameplayTweakCategory.RecommendedForEveryone, ref _recommendedAllEnabled, value, nameof(RecommendedAllEnabled));
    }

    /// <summary>Whether every "Preferences" tweak is currently enabled. Setting it enables/disables all of them together.</summary>
    public bool PreferenceAllEnabled
    {
        get => _preferenceAllEnabled;
        set => SetCategoryToggle(GameplayTweakCategory.Preference, ref _preferenceAllEnabled, value, nameof(PreferenceAllEnabled));
    }

    /// <summary>
    /// Re-reads every tweak's current state from client.cfg. Call whenever the Gameplay page
    /// becomes visible again - a tweak changed outside the app (or by restoring a backup) would
    /// otherwise keep showing whatever this instance last knew.
    /// </summary>
    public void RefreshTweaks()
    {
        IReadOnlyList<GameplayTweak> definitions = _configService.GetRecommendedGameplayTweaks();
        IReadOnlyDictionary<string, string> current = _configService.ReadConvars(
            definitions.SelectMany(t => t.Convars.Select(c => c.Convar)).ToList());

        IReadOnlyList<GameplayTweakRow> BuildRows(GameplayTweakCategory category) => definitions
            .Where(t => t.Category == category)
            .Select(t => new GameplayTweakRow(Localization[t.LabelKey], Localization[t.DescriptionKey], t, _configService, IsApplied(t, current)))
            .ToList();

        RecommendedTweaks = BuildRows(GameplayTweakCategory.RecommendedForEveryone);
        PreferenceTweaks = BuildRows(GameplayTweakCategory.Preference);

        // Each row can flip independently (not just via the master toggle), so the master toggles
        // need to stay in sync with whatever the rows end up at.
        foreach (GameplayTweakRow row in RecommendedTweaks.Concat(PreferenceTweaks))
            row.PropertyChanged += OnTweakRowPropertyChanged;

        RecomputeMasterToggles();
    }

    /// <summary>Whether every one of a tweak's convars is currently set to its recommended (enabled) value.</summary>
    private static bool IsApplied(GameplayTweak tweak, IReadOnlyDictionary<string, string> current) =>
        tweak.Convars.All(c => current.TryGetValue(c.Convar, out string? value) && string.Equals(value, c.EnabledValue, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Backs the two master toggles: writes every tweak in <paramref name="category"/> to its
    /// enabled/disabled value in one client.cfg write, then reloads the row lists. Reverts the
    /// toggle if the write fails (e.g. Rust is running).
    /// </summary>
    private void SetCategoryToggle(GameplayTweakCategory category, ref bool field, bool value, string propertyName)
    {
        if (field == value)
            return;

        field = value;
        OnPropertyChanged(propertyName);

        Dictionary<string, string> convars = new(StringComparer.OrdinalIgnoreCase);
        foreach (GameplayTweak tweak in _configService.GetRecommendedGameplayTweaks().Where(t => t.Category == category))
            foreach (ConvarValue convar in tweak.Convars)
                convars[convar.Convar] = value ? convar.EnabledValue : convar.DisabledValue;

        if (convars.Count > 0 && _configService.SetConvars(convars, createBackup: false))
        {
            RefreshTweaks();
            return;
        }

        field = !value;
        OnPropertyChanged(propertyName);
    }

    /// <summary>Keeps the master toggles in sync whenever an individual row flips on its own.</summary>
    private void OnTweakRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameplayTweakRow.IsEnabled))
            RecomputeMasterToggles();
    }

    /// <summary>Sets both master toggles to whether every row in their category is currently enabled, without writing anything.</summary>
    private void RecomputeMasterToggles()
    {
        bool recommendedAll = RecommendedTweaks.Count > 0 && RecommendedTweaks.All(r => r.IsEnabled);
        if (recommendedAll != _recommendedAllEnabled)
        {
            _recommendedAllEnabled = recommendedAll;
            OnPropertyChanged(nameof(RecommendedAllEnabled));
        }

        bool preferenceAll = PreferenceTweaks.Count > 0 && PreferenceTweaks.All(r => r.IsEnabled);
        if (preferenceAll != _preferenceAllEnabled)
        {
            _preferenceAllEnabled = preferenceAll;
            OnPropertyChanged(nameof(PreferenceAllEnabled));
        }
    }

    /// <summary>Re-evaluates <see cref="CanToggleTweaks"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanToggleTweaks));
    }
}