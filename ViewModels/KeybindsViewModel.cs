using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using System.Threading.Tasks;
using RustOptimizer.Service;
using System;
using System.Linq;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Keybinds page: every current bind line from keys.cfg grouped into collapsible category
/// sections (all collapsed by default, so the page opens as a short table of contents rather than one
/// long scroll) - typing in the search box bypasses grouping entirely and shows a flat, category-tagged
/// list of just the matches. Plus two ways to add a bind - "Choose from list" (a searchable catalog
/// with a description preview) and "Create manually" (a multi-stage macro builder for toggle-cycle
/// binds) - and per-row Change/Remove. All three write paths go through the same
/// <see cref="IKeybindsService"/> guards <see cref="IConfigService.SetConvars"/> already applies to
/// client.cfg - refused while Rust is running, backed up first.
/// </summary>
public sealed class KeybindsViewModel : ViewModelBase
{
    private const string CategoryOther = "KeybindCategoryOther";

    /// <summary>The <c>no_input</c> command - Rust's own default for unused keys (e.g. numpad on a keyboard with none). Deliberately hidden - it's noise, not a bind anyone set.</summary>
    private const string NoInputCommand = "no_input";

    // Display order for both the grouped view and the flat search view - fixed rather than whatever
    // order keys.cfg happens to have its lines in, so the page reads the same way every time.
    private static readonly string[] CategoryOrder =
    [
        "KeybindCategoryMovement", "KeybindCategoryCombat", "KeybindCategoryInteraction",
        "KeybindCategoryInventory", "KeybindCategoryCommunication", "KeybindCategoryCameraMap",
        "KeybindCategoryBuildingVehicles", "KeybindCategoryAudioToggles", "KeybindCategoryMusic",
        "KeybindCategoryConsole", CategoryOther
    ];

    private readonly IKeybindsService _keybinds;
    private readonly IDialogService _dialogs;
    private readonly SidebarViewModel _sidebar;
    private readonly HashSet<string> _expandedCategories = [];

    private IReadOnlyList<KeybindRow> _rows = [];
    private IReadOnlyList<KeybindCategoryGroup> _groups = [];
    private IReadOnlyList<KeybindRow> _filteredRows = [];
    private string _searchText = "";
    private string _statusText = "";

    /// <summary>Creates the view model and loads the current bindings.</summary>
    public KeybindsViewModel(ILocalizationService localization, IKeybindsService keybinds, IDialogService dialogs, SidebarViewModel sidebar)
        : base(localization)
    {
        _keybinds = keybinds;
        _dialogs = dialogs;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        ChooseFromListCommand = new RelayCommand(() => _ = AddFromCatalogAsync());
        CreateManuallyCommand = new RelayCommand(() => _ = AddFromBuilderAsync());
        ChangeKeyCommand = new RelayCommand<KeybindRow>(row => { if (row is not null) _ = ChangeKeyAsync(row); });
        RemoveKeyCommand = new RelayCommand<KeybindRow>(row => { if (row is not null) _ = RemoveKeyAsync(row); });
        ToggleGroupCommand = new RelayCommand<KeybindCategoryGroup>(group => { if (group is not null) ToggleGroup(group); });

        RefreshRows();
    }

    /// <summary>Whether the search box has anything typed - switches the view between grouped browsing and a flat filtered list.</summary>
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Every category, collapsed by default, shown while <see cref="IsSearching"/> is false.</summary>
    public IReadOnlyList<KeybindCategoryGroup> Groups
    {
        get => _groups;
        private set => SetProperty(ref _groups, value);
    }

    /// <summary>Every bind row matching <see cref="SearchText"/>, flat and category-tagged, shown while <see cref="IsSearching"/> is true.</summary>
    public IReadOnlyList<KeybindRow> FilteredRows
    {
        get => _filteredRows;
        private set => SetProperty(ref _filteredRows, value);
    }

    /// <summary>Text typed to filter the list - matches against the row's action text, raw command, key text, or category, case-insensitive substring.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            OnPropertyChanged(nameof(IsSearching));
            ApplyFilter();
        }
    }

    /// <summary>Status line shown after Add/Change/Remove - success, or why it didn't happen.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    // TEMP (requested for local UI testing while Rust is running - revert before shipping): the real
    // write guard lives in KeybindsService.SetKeyBinding/RemoveKeyBinding independently of this flag,
    // so leaving this true doesn't risk keys.cfg - it only unblocks browsing the dialogs. Flip back to
    // "!_sidebar.IsRustRunning" once done testing.
    private const bool DebugBypassRustGuard = true;

    /// <summary>Whether writes are currently allowed - keys.cfg can't be safely written while Rust has it open, same guard the Gameplay page uses for client.cfg.</summary>
    public bool CanEdit => DebugBypassRustGuard || !_sidebar.IsRustRunning;

    /// <summary>Opens the searchable action catalog, picks a key, then adds the bind.</summary>
    public RelayCommand ChooseFromListCommand { get; }

    /// <summary>Opens the manual macro builder, picks a key, then adds the bind.</summary>
    public RelayCommand CreateManuallyCommand { get; }

    /// <summary>Opens the key-capture dialog to move an existing row to a different key.</summary>
    public RelayCommand<KeybindRow> ChangeKeyCommand { get; }

    /// <summary>Removes an existing row's binding entirely.</summary>
    public RelayCommand<KeybindRow> RemoveKeyCommand { get; }

    /// <summary>Expands or collapses a category section.</summary>
    public RelayCommand<KeybindCategoryGroup> ToggleGroupCommand { get; }

    /// <summary>Re-reads keys.cfg. Call whenever the page becomes visible again, same as other pages refresh on attach.</summary>
    public void RefreshRows()
    {
        _rows = _keybinds.GetKeyBindings()
            .Where(binding => binding.Command != NoInputCommand)
            .Select(ToRow)
            .OrderBy(row => Array.IndexOf(CategoryOrder, row.Binding.CategoryKey ?? CategoryOther))
            .ToList();

        RebuildGroups();
        ApplyFilter();
    }

    /// <summary>Flips a category's expanded state and rebuilds <see cref="Groups"/>.</summary>
    private void ToggleGroup(KeybindCategoryGroup group)
    {
        if (!_expandedCategories.Remove(group.CategoryKey))
            _expandedCategories.Add(group.CategoryKey);

        RebuildGroups();
    }

    /// <summary>Rebuilds <see cref="Groups"/> from <see cref="_rows"/> and the current expand/collapse state.</summary>
    private void RebuildGroups()
    {
        Groups = _rows
            .GroupBy(row => row.Binding.CategoryKey ?? CategoryOther)
            .OrderBy(group => Array.IndexOf(CategoryOrder, group.Key))
            .Select(group => new KeybindCategoryGroup(group.Key, Localization[group.Key], group.ToList(), _expandedCategories.Contains(group.Key)))
            .ToList();
    }

    /// <summary>Re-applies <see cref="SearchText"/> against the loaded rows.</summary>
    private void ApplyFilter()
    {
        if (!IsSearching)
        {
            FilteredRows = [];
            return;
        }

        FilteredRows = _rows
            .Where(row =>
                row.ActionText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || row.Binding.Command.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || row.KeyText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || row.CategoryLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Converts a raw binding into its display row - a friendly action name where known, the raw command otherwise.</summary>
    private KeybindRow ToRow(KeyBindingRow binding)
    {
        string actionText = binding.ActionLabelKey is { } key ? Localization[key] : binding.Command;
        string keyText = binding.Token.Modifier is { } modifier
            ? $"{KeyTokenCatalog.ModifierDisplayName(modifier) ?? modifier} + {KeyTokenCatalog.DisplayNameFor(binding.Token.Key)}"
            : KeyTokenCatalog.DisplayNameFor(binding.Token.Key);
        string categoryLabel = Localization[binding.CategoryKey ?? CategoryOther];

        return new KeybindRow(binding, actionText, keyText, categoryLabel);
    }

    /// <summary>Opens the catalog dialog, then captures a key for whatever command was picked.</summary>
    private async Task AddFromCatalogAsync()
    {
        if (await _dialogs.ShowBindCatalogAsync(Localization, _keybinds.GetKnownActions()) is not { } command)
            return;

        await FinishAddAsync(command);
    }

    /// <summary>Opens the manual macro builder, then captures a key for the assembled command.</summary>
    private async Task AddFromBuilderAsync()
    {
        if (await _dialogs.ShowBindBuilderAsync(Localization) is not { } command)
            return;

        await FinishAddAsync(command);
    }

    /// <summary>Shared tail of both add paths: capture a key, then write the binding.</summary>
    private async Task FinishAddAsync(string command)
    {
        if (await _dialogs.ShowKeyCaptureAsync(Localization, _keybinds.GetKeyBindings(), excludeToken: null) is not { } token)
            return;

        bool success = _keybinds.SetKeyBinding(token, command);
        StatusText = Localization[success ? "KeybindAdded" : "KeybindWriteFailed"];

        if (success)
            RefreshRows();
    }

    /// <summary>Picks a new key for an existing row's command, moving it there.</summary>
    private async Task ChangeKeyAsync(KeybindRow row)
    {
        if (await _dialogs.ShowKeyCaptureAsync(Localization, _keybinds.GetKeyBindings(), row.Binding.Token) is not { } newToken)
            return;

        if (newToken == row.Binding.Token)
            return;

        bool success = _keybinds.SetKeyBinding(newToken, row.Binding.Command)
            && _keybinds.RemoveKeyBinding(row.Binding.Token, createBackup: false);

        StatusText = Localization[success ? "KeybindChanged" : "KeybindWriteFailed"];
        if (success)
            RefreshRows();
    }

    /// <summary>Removes an existing row's binding entirely.</summary>
    private async Task RemoveKeyAsync(KeybindRow row)
    {
        bool success = await Task.Run(() => _keybinds.RemoveKeyBinding(row.Binding.Token));
        StatusText = Localization[success ? "KeybindRemoved" : "KeybindWriteFailed"];
        if (success)
            RefreshRows();
    }

    /// <summary>Re-evaluates <see cref="CanEdit"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanEdit));
    }
}