using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;

namespace RustOptimizer.ViewModels;

/// <summary>Drives the Utilities page: a static list of external Rust resources.</summary>
public sealed class UtilitiesViewModel : ViewModelBase
{
    private IReadOnlyList<UtilityResourceRow> _resources = [];

    public UtilitiesViewModel(ILocalizationService localization) : base(localization)
    {
        OpenResourceCommand = new RelayCommand<string>(url =>
        {
            if (url is not null)
                Utility.OpenUrl(url);
        });

        RefreshResources();

        // Descriptions are resolved from UtilityResourceCatalog's localization keys up front, so
        // they need to be re-resolved (and the whole list re-bound) on every language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                RefreshResources();
        };
    }

    /// <summary>The external resources shown as cards, with descriptions resolved for the current language.</summary>
    public IReadOnlyList<UtilityResourceRow> Resources
    {
        get => _resources;
        private set => SetProperty(ref _resources, value);
    }

    /// <summary>Opens a resource's URL (passed as the command parameter) in the default browser.</summary>
    public RelayCommand<string> OpenResourceCommand { get; }

    private void RefreshResources()
        => Resources = UtilityResourceCatalog.All
            .Select(r => new UtilityResourceRow(r.Name, Localization[r.DescriptionKey], r.Url, r.IconKind))
            .ToList();
}