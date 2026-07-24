using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using System.Threading.Tasks;
using RustOptimizer.Service.Logging;
using RustOptimizer.Interface;
using System.ComponentModel;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Graphics page: a profile dropdown (three built-in presets plus the user's saved custom
/// profiles) over a flat list of simplified quality sliders (see
/// <see cref="RustOptimizer.Service.RecommendedGraphicsSliders"/>) that write Rust's client.cfg the
/// same way Gameplay's tweaks do. Picking a profile applies it live; the sliders can then be nudged
/// and the result saved over a custom profile or saved as a new one. The three built-ins can't be
/// overridden, renamed, or deleted. GPU/driver/display specs already live on the System page.
/// </summary>
public sealed class GraphicsViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IAppSettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly SidebarViewModel _sidebar;
    private IReadOnlyList<GraphicsSliderRow> _sliders = [];
    private IReadOnlyList<GraphicsProfileOption> _profiles = [];
    private GraphicsProfileOption? _selectedProfile;
    private bool _isModified;
    private string _profileStatusText = "";

    // Set while the view model, not the user, changes SelectedProfile - suppresses the apply-to-config
    // side effect so reconciling the dropdown to what client.cfg already holds doesn't re-write it.
    private bool _suppressApply;

    /// <summary>Creates the view model, loads every slider's current tier, and selects the profile that matches.</summary>
    public GraphicsViewModel(ILocalizationService localization, IConfigService configService, IAppSettingsService settings,
        IDialogService dialogs, SidebarViewModel sidebar)
        : base(localization)
    {
        _configService = configService;
        _settings = settings;
        _dialogs = dialogs;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        SaveProfileCommand = new RelayCommand(() => _ = SaveAsync(), () => _isModified);
        RenameProfileCommand = new RelayCommand(() => _ = RenameAsync(), () => _selectedProfile is { IsBuiltIn: false });
        DeleteProfileCommand = new RelayCommand(() => _ = DeleteAsync(), () => _selectedProfile is { IsBuiltIn: false });

        RefreshSliders();

        // Slider titles/tier labels and the built-in profile names are resolved from localization keys
        // up front, so the whole page's row/profile lists need rebuilding (not just re-binding) on a
        // language switch. A resolved status message from the old language is cleared rather than left stale.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
            {
                ProfileStatusText = "";
                RefreshSliders();
            }
        };
    }

    /// <summary>Every quality slider shown on the page, in display order.</summary>
    public IReadOnlyList<GraphicsSliderRow> Sliders
    {
        get => _sliders;
        private set => SetProperty(ref _sliders, value);
    }

    /// <summary>The profiles offered in the dropdown: the three built-ins first, then any custom profiles.</summary>
    public IReadOnlyList<GraphicsProfileOption> Profiles
    {
        get => _profiles;
        private set => SetProperty(ref _profiles, value);
    }

    /// <summary>
    /// The profile the dropdown currently points at. Setting it in response to a user pick applies
    /// that profile to client.cfg; the view model reconciling the selection to what's already on disk
    /// goes through <see cref="_suppressApply"/> so it doesn't re-write anything.
    /// </summary>
    public GraphicsProfileOption? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
                return;

            OnPropertyChanged(nameof(IsBuiltInSelected));
            UpdateCommandStates();

            if (!_suppressApply && value is not null)
                ApplyProfile(value);
        }
    }

    /// <summary>
    /// Whether the selected profile is a built-in - <see langword="false"/> when nothing is selected
    /// (a custom/unmatched state). Drives the "Built-in" tag; bound through here rather than
    /// <c>SelectedProfile.IsBuiltIn</c> so a null selection reliably hides the tag instead of a broken
    /// binding path defaulting the tag's visibility to true.
    /// </summary>
    public bool IsBuiltInSelected => _selectedProfile is { IsBuiltIn: true };

    /// <summary>
    /// Whether the sliders no longer match <see cref="SelectedProfile"/> - either the user has nudged
    /// a slider since selecting it, or client.cfg matches no saved profile at all. Drives the "unsaved
    /// changes" hint and whether <see cref="SaveProfileCommand"/> can run.
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        private set
        {
            if (SetProperty(ref _isModified, value))
                UpdateCommandStates();
        }
    }

    /// <summary>A short status line shown after applying, saving, renaming, or deleting a profile.</summary>
    public string ProfileStatusText
    {
        get => _profileStatusText;
        private set => SetProperty(ref _profileStatusText, value);
    }

    /// <summary>
    /// Saves the current slider state: overwrites the selected custom profile, or - when deriving from
    /// a built-in - prompts for a name and creates a new custom profile.
    /// </summary>
    public RelayCommand SaveProfileCommand { get; }

    /// <summary>Prompts for a new name for the selected custom profile.</summary>
    public RelayCommand RenameProfileCommand { get; }

    /// <summary>Deletes the selected custom profile after confirmation.</summary>
    public RelayCommand DeleteProfileCommand { get; }

    /// <summary>Whether sliders can be interacted with - client.cfg can't be safely written while Rust has it open.</summary>
    public bool CanApplySliders => !_sidebar.IsRustRunning;

    /// <summary>
    /// Re-reads every slider's current tier from client.cfg, rebuilds the profile dropdown, and
    /// selects whichever profile the current settings match. Call whenever the Graphics page becomes
    /// visible again - a change made outside the app (or by restoring a backup) would otherwise keep
    /// showing whatever this instance last knew.
    /// </summary>
    public void RefreshSliders()
    {
        BuildSliderRows();
        RebuildProfiles();
        SyncSelectionToCurrent();
    }

    /// <summary>
    /// Reads client.cfg into a fresh set of slider rows, resubscribing to their changes.
    /// <paramref name="knownTierBySlider"/>, when given, is the tier map just applied to client.cfg -
    /// each covered slider is set to that exact tier instead of being re-derived from the file. That
    /// matters because Shadow Quality and Water Quality's Low and Medium tiers write identical
    /// convars, so re-deriving from client.cfg alone can't tell them apart and always resolves to
    /// whichever sorts first (Low) - silently showing the wrong tier for a profile that specified
    /// Medium. On a cold load (no profile was just applied), there's no such known map, so this falls
    /// back to the same convar-matching used everywhere else.
    /// </summary>
    private void BuildSliderRows(IReadOnlyDictionary<string, string>? knownTierBySlider = null)
    {
        foreach (GraphicsSliderRow row in Sliders)
            row.PropertyChanged -= OnSliderRowChanged;

        IReadOnlyList<GraphicsSlider> definitions = _configService.GetGraphicsSliders();
        List<string> allConvars = definitions
            .SelectMany(slider => slider.Tiers.SelectMany(tier => tier.Values.Select(v => v.Convar)))
            .Distinct()
            .ToList();
        IReadOnlyDictionary<string, string> current = _configService.ReadConvars(allConvars);

        List<GraphicsSliderRow> rows = definitions.Select(slider => BuildRow(slider, current, knownTierBySlider)).ToList();
        foreach (GraphicsSliderRow row in rows)
            row.PropertyChanged += OnSliderRowChanged;

        Sliders = rows;
    }

    /// <summary>
    /// Builds one slider's row. Prefers the known tier from <paramref name="knownTierBySlider"/> when
    /// this slider is covered by it; otherwise matches its current client.cfg values against every
    /// known tier (ambiguous for sliders with duplicate-convar tiers - see <see cref="BuildSliderRows"/>).
    /// </summary>
    private GraphicsSliderRow BuildRow(GraphicsSlider slider, IReadOnlyDictionary<string, string> current, IReadOnlyDictionary<string, string>? knownTierBySlider)
    {
        List<GraphicsTierOption> tierOptions = slider.Tiers
            .Select(tier => new GraphicsTierOption(Localization[tier.LabelKey], tier))
            .ToList();

        int matchedIndex = knownTierBySlider is not null && knownTierBySlider.TryGetValue(slider.PreviewId, out string? knownTierId)
            ? tierOptions.FindIndex(option => string.Equals(option.Tier.PreviewId, knownTierId, StringComparison.OrdinalIgnoreCase))
            : tierOptions.FindIndex(option => Matches(option.Tier, current));

        return new GraphicsSliderRow(Localization[slider.TitleKey], slider.PreviewId, tierOptions, _configService,
            matchedIndex >= 0 ? matchedIndex : null);
    }

    /// <summary>Whether every convar in <paramref name="tier"/> currently matches its value in client.cfg.</summary>
    private static bool Matches(GraphicsSliderTier tier, IReadOnlyDictionary<string, string> current) =>
        tier.Values.All(setting => current.TryGetValue(setting.Convar, out string? value)
            && string.Equals(value, setting.Value, StringComparison.OrdinalIgnoreCase));

    /// <summary>Rebuilds the dropdown's options: the three built-in presets, then the user's saved profiles.</summary>
    private void RebuildProfiles()
    {
        List<GraphicsProfileOption> options =
        [
            GraphicsProfileOption.ForBuiltIn(Localization["ProfileLowEndPc"], ConfigPreset.LowEndPc, _configService.GetPresetProfile(ConfigPreset.LowEndPc)),
            GraphicsProfileOption.ForBuiltIn(Localization["ProfileCompetitive"], ConfigPreset.Competitive, _configService.GetPresetProfile(ConfigPreset.Competitive)),
            GraphicsProfileOption.ForBuiltIn(Localization["ProfileCinematic"], ConfigPreset.Cinematic, _configService.GetPresetProfile(ConfigPreset.Cinematic))
        ];

        foreach (GraphicsProfile profile in _settings.Current.GraphicsProfiles)
            options.Add(GraphicsProfileOption.ForCustom(profile));

        Profiles = options;
    }

    /// <summary>
    /// Points the dropdown at whichever profile the current slider state matches (or none, leaving it
    /// blank and flagged as unsaved), without re-applying anything to client.cfg.
    /// </summary>
    private void SyncSelectionToCurrent()
    {
        GraphicsProfileOption? match = Profiles.FirstOrDefault(CurrentMatchesProfile);

        _suppressApply = true;
        SelectedProfile = match;
        _suppressApply = false;

        IsModified = match is null;
    }

    /// <summary>
    /// Applies a profile to client.cfg - a built-in via its preset, a custom via its tier picks - then
    /// re-reads the sliders and reconciles the dropdown to whatever ended up on disk (which is the
    /// selected profile on success, or the real state again if the write was refused).
    /// </summary>
    private void ApplyProfile(GraphicsProfileOption option)
    {
        // No backup on a profile switch - switching is a routine, reversible pick (Backup & Restore
        // covers deliberate snapshots), same reasoning as the per-slider writes.
        bool applied = option.IsBuiltIn
            ? _configService.ApplyPreset(option.Preset, createBackup: false)
            : _configService.ApplyGraphicsProfile(option.TierBySlider, createBackup: false);

        AppLog.Debug("GraphicsViewModel", $"Applied profile '{option.Name}' (builtIn={option.IsBuiltIn}): success={applied}.");
        ProfileStatusText = Localization[applied ? "ProfileApplied" : "PresetApplyFailed"];

        // On success, trust the tier map we just wrote (see BuildSliderRows) instead of re-deriving each
        // slider from client.cfg. On failure, there's nothing to trust - re-derive honestly instead.
        BuildSliderRows(applied ? option.TierBySlider : null);

        // Reconcile the dropdown to what's actually on disk. Built-ins sort first, so settings that
        // equal a built-in resolve to that built-in: a set of values identical to a preset *is* that
        // preset, so it's shown as such rather than as a redundant custom copy.
        SyncSelectionToCurrent();
    }

    /// <summary>Whether the current slider state matches every tier <paramref name="option"/> prescribes.</summary>
    private bool CurrentMatchesProfile(GraphicsProfileOption option)
    {
        foreach (GraphicsSliderRow row in Sliders)
        {
            // A slider whose client.cfg value matches no tier can't match any profile.
            if (row.IsCustom)
                return false;

            if (!option.TierBySlider.TryGetValue(row.PreviewId, out string? tier)
                || !row.CurrentTierValuesMatch(tier))
                return false;
        }

        return true;
    }

    /// <summary>Captures where every slider handle currently sits: slider PreviewId → tier PreviewId.</summary>
    private Dictionary<string, string> CaptureCurrentSelection()
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (GraphicsSliderRow row in Sliders)
            map[row.PreviewId] = row.SelectedTierPreviewId;
        return map;
    }

    /// <summary>
    /// Saves the current slider state. A selected custom profile is overwritten in place; a built-in
    /// (or an unsaved custom state) can't be overwritten, so the user is prompted for a name and a new
    /// custom profile is created instead.
    /// </summary>
    private async Task SaveAsync()
    {
        if (_selectedProfile is { IsBuiltIn: false, Custom: { } custom })
        {
            // Mutate the existing dictionary in place: the option's TierBySlider aliases it, so both
            // stay in sync without rebuilding the dropdown and losing the current selection's identity.
            custom.Sliders.Clear();
            foreach (KeyValuePair<string, string> kv in CaptureCurrentSelection())
                custom.Sliders[kv.Key] = kv.Value;

            _settings.Save();
            AppLog.Debug("GraphicsViewModel", $"Overwrote profile '{custom.Name}'.");
            RecomputeModified();
            ProfileStatusText = Localization["ProfileSaved"];
            return;
        }

        string? name = await _dialogs.ShowPromptAsync(Localization, Localization["ProfileSaveAsTitle"],
            Localization["ProfileNamePrompt"], Localization["Save"], "");
        if (string.IsNullOrWhiteSpace(name))
            return;

        GraphicsProfile profile = new() { Name = name, Sliders = CaptureCurrentSelection() };
        _settings.Current.GraphicsProfiles.Add(profile);
        _settings.Save();
        AppLog.Debug("GraphicsViewModel", $"Created profile '{profile.Name}'.");

        RebuildProfiles();
        SelectWithoutApplying(Profiles.FirstOrDefault(option => option.Custom == profile));
        RecomputeModified();
        ProfileStatusText = Localization["ProfileSaved"];
    }

    /// <summary>Prompts for a new name for the selected custom profile.</summary>
    private async Task RenameAsync()
    {
        if (_selectedProfile is not { IsBuiltIn: false, Custom: { } custom })
            return;

        string? name = await _dialogs.ShowPromptAsync(Localization, Localization["ProfileRenameTitle"],
            Localization["ProfileNamePrompt"], Localization["Save"], custom.Name);
        if (string.IsNullOrWhiteSpace(name) || name == custom.Name)
            return;

        AppLog.Debug("GraphicsViewModel", $"Renamed profile '{custom.Name}' to '{name}'.");
        custom.Name = name;
        _settings.Save();

        RebuildProfiles();
        SelectWithoutApplying(Profiles.FirstOrDefault(option => option.Custom == custom));
        RecomputeModified();
    }

    /// <summary>Deletes the selected custom profile after confirmation, leaving client.cfg untouched.</summary>
    private async Task DeleteAsync()
    {
        if (_selectedProfile is not { IsBuiltIn: false, Custom: { } custom })
            return;

        bool confirmed = await _dialogs.ShowConfirmAsync(Localization, Localization["ProfileDeleteTitle"],
            string.Format(Localization["ProfileDeleteMessageFormat"], custom.Name), Localization["Delete"], isDestructive: true);
        if (!confirmed)
            return;

        _settings.Current.GraphicsProfiles.Remove(custom);
        _settings.Save();
        AppLog.Debug("GraphicsViewModel", $"Deleted profile '{custom.Name}'.");

        RebuildProfiles();
        SyncSelectionToCurrent();
        ProfileStatusText = Localization["ProfileDeleted"];
    }

    /// <summary>Selects a profile in the dropdown without re-applying it (client.cfg already matches it).</summary>
    private void SelectWithoutApplying(GraphicsProfileOption? option)
    {
        _suppressApply = true;
        SelectedProfile = option;
        _suppressApply = false;
    }

    /// <summary>
    /// Reconciles the dropdown after a slider is nudged, via the same <see cref="SyncSelectionToCurrent"/>
    /// every other path uses: snaps to whichever profile (built-in or custom) the new slider state
    /// exactly matches, or blanks the selection - which shows the "Custom" placeholder - when nothing
    /// matches. A slider drag has already written client.cfg directly, so this only ever reconciles
    /// the dropdown's display; it never re-applies anything.
    /// </summary>
    private void OnSliderRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GraphicsSliderRow.SelectedTierIndex))
            SyncSelectionToCurrent();
    }

    /// <summary>
    /// Recomputes whether the sliders still match <see cref="SelectedProfile"/> - true (modified) when
    /// nothing is selected, when a slider is off-tier, or when any slider differs from the selection.
    /// </summary>
    private void RecomputeModified()
        => IsModified = _selectedProfile is null || !CurrentMatchesProfile(_selectedProfile);

    /// <summary>Re-evaluates <see cref="CanApplySliders"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanApplySliders));
    }

    /// <summary>Re-queries every profile command's availability after a selection or modified-state change.</summary>
    private void UpdateCommandStates()
    {
        SaveProfileCommand.RaiseCanExecuteChanged();
        RenameProfileCommand.RaiseCanExecuteChanged();
        DeleteProfileCommand.RaiseCanExecuteChanged();
    }
}
