using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using System.Linq;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Graphics page: a flat list of simplified quality sliders (see
/// <see cref="RustOptimizer.Service.RecommendedGraphicsSliders"/>) that write Rust's client.cfg the
/// same way Gameplay's tweaks do. GPU/driver/display specs already live on the System page.
/// </summary>
public sealed class GraphicsViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly SidebarViewModel _sidebar;
    private IReadOnlyList<GraphicsSliderRow> _sliders = [];

    /// <summary>Creates the view model and loads every slider's current tier.</summary>
    public GraphicsViewModel(ILocalizationService localization, IConfigService configService, SidebarViewModel sidebar)
        : base(localization)
    {
        _configService = configService;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        RefreshSliders();

        // Slider titles/tier labels are resolved from RecommendedGraphicsSliders' localization keys
        // up front, so the whole row list needs rebuilding (not just re-binding) on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                RefreshSliders();
        };
    }

    /// <summary>Every quality slider shown on the page, in display order.</summary>
    public IReadOnlyList<GraphicsSliderRow> Sliders
    {
        get => _sliders;
        private set => SetProperty(ref _sliders, value);
    }

    /// <summary>Whether sliders can be interacted with - client.cfg can't be safely written while Rust has it open.</summary>
    public bool CanApplySliders => !_sidebar.IsRustRunning;

    /// <summary>
    /// Re-reads every slider's current tier from client.cfg. Call whenever the Graphics page
    /// becomes visible again - a change made outside the app (or by restoring a backup) would
    /// otherwise keep showing whatever this instance last knew.
    /// </summary>
    public void RefreshSliders()
    {
        IReadOnlyList<GraphicsSlider> definitions = _configService.GetGraphicsSliders();
        List<string> allConvars = definitions
            .SelectMany(slider => slider.Tiers.SelectMany(tier => tier.Values.Select(v => v.Convar)))
            .Distinct()
            .ToList();
        IReadOnlyDictionary<string, string> current = _configService.ReadConvars(allConvars);

        Sliders = definitions.Select(slider => BuildRow(slider, current)).ToList();
    }

    /// <summary>Builds one slider's row, matching its current client.cfg values against every known tier.</summary>
    private GraphicsSliderRow BuildRow(GraphicsSlider slider, IReadOnlyDictionary<string, string> current)
    {
        List<GraphicsTierOption> tierOptions = slider.Tiers
            .Select(tier => new GraphicsTierOption(Localization[tier.LabelKey], tier))
            .ToList();

        int matchedIndex = tierOptions.FindIndex(option => Matches(option.Tier, current));
        return new GraphicsSliderRow(Localization[slider.TitleKey], slider.PreviewId, tierOptions, _configService,
            matchedIndex >= 0 ? matchedIndex : null);
    }

    /// <summary>Whether every convar in <paramref name="tier"/> currently matches its value in client.cfg.</summary>
    private static bool Matches(GraphicsSliderTier tier, IReadOnlyDictionary<string, string> current) =>
        tier.Values.All(setting => current.TryGetValue(setting.Convar, out string? value)
            && string.Equals(value, setting.Value, System.StringComparison.OrdinalIgnoreCase));

    /// <summary>Re-evaluates <see cref="CanApplySliders"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanApplySliders));
    }
}