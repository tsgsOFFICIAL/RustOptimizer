using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the Smart Optimization confirm prompt: a grouped, read-only list of exactly what's about
/// to change (built once from the <see cref="SmartOptimizationPlan"/> the caller already computed),
/// with an Apply/Cancel footer. Applying is owned here, same as <see cref="ClearCacheDialogViewModel"/>
/// owns its own cleanup run - the prompt swaps to a brief progress state while
/// <see cref="ISmartOptimizationService.ApplyAsync"/> runs (which can take a moment if a Network
/// change needs an elevated re-launch), then closes with the outcome. <see cref="CloseRequested"/>
/// carries <see langword="null"/> if the user cancelled before applying anything.
/// </summary>
public sealed class SmartOptimizationDialogViewModel : ViewModelBase
{
    private readonly ISmartOptimizationService _smartOptimization;
    private readonly SmartOptimizationPlan _plan;
    private bool _isApplying;

    public SmartOptimizationDialogViewModel(ILocalizationService localization, ISmartOptimizationService smartOptimization, SmartOptimizationPlan plan)
        : base(localization)
    {
        _smartOptimization = smartOptimization;
        _plan = plan;

        SystemChangesText = BulletTextFor(OptimizationCategory.System);
        NetworkChangesText = BulletTextFor(OptimizationCategory.Network);
        GameplayChangesText = BulletTextFor(OptimizationCategory.Gameplay);

        ApplyCommand = new RelayCommand(() => _ = ApplyAsync());
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(null));
    }

    /// <summary>Raised when the prompt should close, carrying the applied outcome, or <see langword="null"/> if the user cancelled first.</summary>
    public event Action<SmartOptimizationOutcome?>? CloseRequested;

    /// <summary>One bullet line per outstanding System tweak, already localized and newline-joined.</summary>
    public string SystemChangesText { get; }

    /// <summary>One bullet line per outstanding Network tweak, already localized and newline-joined.</summary>
    public string NetworkChangesText { get; }

    /// <summary>One bullet line per outstanding "recommended for everyone" Gameplay tweak, already localized and newline-joined.</summary>
    public string GameplayChangesText { get; }

    public bool HasSystemChanges => SystemChangesText.Length > 0;
    public bool HasNetworkChanges => NetworkChangesText.Length > 0;
    public bool HasGameplayChanges => GameplayChangesText.Length > 0;

    /// <summary>Whether a graphics preset is being proposed - only when nothing recognized is already applied, see <see cref="SmartOptimizationPlan.RecommendedGraphicsPreset"/>.</summary>
    public bool HasGraphicsChange => _plan.RecommendedGraphicsPreset is not null;

    /// <summary>The recommended preset's display name, e.g. "Competitive".</summary>
    public string GraphicsPresetName => _plan.RecommendedGraphicsPreset switch
    {
        ConfigPreset.LowEndPc => Localization["ProfileLowEndPc"],
        ConfigPreset.Competitive => Localization["ProfileCompetitive"],
        ConfigPreset.Cinematic => Localization["ProfileCinematic"],
        _ => ""
    };

    /// <summary>The graphics change's bullet line, e.g. "• Graphics Profile → Competitive".</summary>
    public string GraphicsChangeText => $"• {string.Format(Localization["SmartOptimizationGraphicsChangeFormat"], GraphicsPresetName)}";

    /// <summary>One-line explanation of why that preset was picked, e.g. "32 GB RAM and a 165 Hz display detected".</summary>
    public string GraphicsReasonText => _plan.GraphicsPresetReasonKey is { } key
        ? string.Format(Localization[key], _plan.GraphicsInstalledMemoryGb, _plan.GraphicsMaxRefreshRateHz ?? 0)
        : "";

    /// <summary>Whether any pending change needs administrator rights - shows the elevation notice near the Network group.</summary>
    public bool RequiresElevation => _plan.Changes.Any(c => c.RequiresElevation);

    /// <summary>Whether Rust needs to be closed before the Gameplay/Graphics changes below can actually be written - see <see cref="SmartOptimizationPlan.RequiresClosingRust"/>.</summary>
    public bool RequiresClosingRust => _plan.RequiresClosingRust;

    /// <summary>Total number of changes about to be made, including the graphics preset if proposed - drives the "Apply N changes" button label.</summary>
    public int TotalChangeCount => _plan.Changes.Count + (HasGraphicsChange ? 1 : 0);

    /// <summary>The Apply button's label, e.g. "Apply 4 changes" or "Apply 1 change" for the singular.</summary>
    public string ApplyButtonText => string.Format(Localization[TotalChangeCount == 1 ? "SmartOptimizationApplyOneFormat" : "SmartOptimizationApplyFormat"], TotalChangeCount);

    /// <summary>Whether the plan is currently being applied - swaps the change list for a brief progress state.</summary>
    public bool IsApplying
    {
        get => _isApplying;
        private set
        {
            if (SetProperty(ref _isApplying, value))
                OnPropertyChanged(nameof(IsConfiguring));
        }
    }

    /// <summary>Whether the change list is still being reviewed - the inverse of <see cref="IsApplying"/>.</summary>
    public bool IsConfiguring => !IsApplying;

    /// <summary>Applies the plan.</summary>
    public RelayCommand ApplyCommand { get; }

    /// <summary>Closes the prompt without applying anything.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>Every outstanding change in <paramref name="category"/>, localized and joined as one bullet per line.</summary>
    private string BulletTextFor(OptimizationCategory category) =>
        string.Join(Environment.NewLine, _plan.Changes.Where(c => c.Category == category).Select(c => $"• {Localization[c.LabelKey]}"));

    /// <summary>Runs the plan through <see cref="ISmartOptimizationService.ApplyAsync"/>, then closes with the outcome.</summary>
    private async Task ApplyAsync()
    {
        if (IsApplying)
            return;

        IsApplying = true;
        SmartOptimizationOutcome outcome = await _smartOptimization.ApplyAsync(_plan);
        CloseRequested?.Invoke(outcome);
    }
}