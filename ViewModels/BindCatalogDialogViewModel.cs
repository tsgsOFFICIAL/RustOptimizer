using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the "Choose from list" catalog dialog: every known <see cref="RustAction"/>, searchable by
/// name, category, or raw command, with a preview pane showing the selected action's friendly name,
/// category, description, and the exact command it would bind. <see cref="CloseRequested"/> carries
/// the chosen action's command string, or <see langword="null"/> on cancel.
/// </summary>
public sealed class BindCatalogDialogViewModel : ViewModelBase
{
    private static readonly string[] CategoryOrder =
    [
        "KeybindCategoryMovement", "KeybindCategoryCombat", "KeybindCategoryInteraction",
        "KeybindCategoryInventory", "KeybindCategoryCommunication", "KeybindCategoryCameraMap",
        "KeybindCategoryBuildingVehicles", "KeybindCategoryAudioToggles", "KeybindCategoryMusic",
        "KeybindCategoryConsole", "KeybindCategoryOther"
    ];

    private readonly IReadOnlyList<RustAction> _actions;
    private IReadOnlyList<CatalogActionRow> _filteredRows;
    private string _searchText = "";
    private CatalogActionRow? _selectedRow;

    public BindCatalogDialogViewModel(ILocalizationService localization, IReadOnlyList<RustAction> actions) : base(localization)
    {
        _actions = actions
            .OrderBy(action => Array.IndexOf(CategoryOrder, action.CategoryKey))
            .ToList();

        _filteredRows = BuildRows();
        _selectedRow = _filteredRows.FirstOrDefault();

        SelectCommand = new RelayCommand<CatalogActionRow>(row =>
        {
            if (row is not null)
                CloseRequested?.Invoke(row.Action.Command);
        });
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(null));
    }

    /// <summary>Every catalog action matching <see cref="SearchText"/>.</summary>
    public IReadOnlyList<CatalogActionRow> FilteredRows
    {
        get => _filteredRows;
        private set => SetProperty(ref _filteredRows, value);
    }

    /// <summary>Text typed to filter the list - matches against the action's friendly name, category, or raw command.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            FilteredRows = BuildRows();

            // The old SelectedRow instance won't be in the freshly-built list even if its action is
            // still present (each BuildRows() call makes new CatalogActionRow records) - re-pick the
            // same action if it's still there, otherwise fall back to the first result, so filtering
            // never leaves the preview pane empty and "Use This Action" dead while a match exists.
            SelectedRow = _selectedRow is { } previous
                ? FilteredRows.FirstOrDefault(row => row.Action.Command == previous.Action.Command) ?? FilteredRows.FirstOrDefault()
                : FilteredRows.FirstOrDefault();
        }
    }

    /// <summary>The action currently shown in the preview pane.</summary>
    public CatalogActionRow? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    /// <summary>Confirms picking the given action.</summary>
    public RelayCommand<CatalogActionRow> SelectCommand { get; }

    /// <summary>Closes without picking anything.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>Raised with the chosen command string, or <see langword="null"/> on cancel.</summary>
    public event Action<string?>? CloseRequested;

    private List<CatalogActionRow> BuildRows() =>
        _actions
            .Select(action => new CatalogActionRow(
                action,
                Localization[action.LabelKey],
                Localization[action.CategoryKey],
                Localization[action.DescriptionKey],
                string.IsNullOrWhiteSpace(SearchText)
                    || Localization[action.LabelKey].Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || Localization[action.CategoryKey].Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || action.Command.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            .Where(row => row.IsMatch)
            .ToList();
}