using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using Avalonia.Media;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>One selectable tier on a <see cref="GraphicsSliderRow"/>'s slider - a resolved display label paired with the tier it applies.</summary>
public sealed record GraphicsTierOption(string Label, GraphicsSliderTier Tier);

/// <summary>
/// One row in the Graphics page's quality-slider list, rendered as a discrete 3-stop slider
/// (Low/Medium/High). <see cref="Title"/> and each
/// <see cref="GraphicsTierOption.Label"/> are resolved once by <see cref="GraphicsViewModel"/> -
/// a language switch rebuilds the whole row list.
/// <see cref="SelectedTierIndex"/> is the only mutable, bindable part: changing it writes every
/// convar in the chosen tier to client.cfg immediately, reverting the position if the write fails,
/// and updates <see cref="PreviewImage"/> to match.
/// </summary>
public sealed class GraphicsSliderRow(string title, string previewId, IReadOnlyList<GraphicsTierOption> tiers, IConfigService configService, int? currentTierIndex) : INotifyPropertyChanged
{
    private int _selectedTierIndex = currentTierIndex ?? 0;
    private bool _isCustom = currentTierIndex is null;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The slider's display name (e.g. "Shadow Quality").</summary>
    public string Title { get; } = title;

    /// <summary>Every tier this slider can be set to, in display order (Low/Medium/High) - three per slider.</summary>
    public IReadOnlyList<GraphicsTierOption> Tiers { get; } = tiers;

    /// <summary>The lowest tier's display label, for the label under the slider's left end.</summary>
    public string LowLabel => Tiers[0].Label;

    /// <summary>The middle tier's display label, for the label under the slider's midpoint.</summary>
    public string MediumLabel => Tiers[1].Label;

    /// <summary>The highest tier's display label, for the label under the slider's right end.</summary>
    public string HighLabel => Tiers[2].Label;

    /// <summary>
    /// The slider's current stop, 0-2. Reflects <see cref="Tiers"/>' matching entry once set; while
    /// <see cref="IsCustom"/> is still true, this is just a starting handle position (defaults to 0)
    /// rather than a claim that client.cfg actually matches it - moving the slider always writes,
    /// even if it lands back on the same stop, clearing <see cref="IsCustom"/>.
    /// </summary>
    public int SelectedTierIndex
    {
        get => _selectedTierIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, Tiers.Count - 1);
            if (!_isCustom && _selectedTierIndex == clamped)
                return;

            int previousIndex = _selectedTierIndex;
            bool previousCustom = _isCustom;
            SetSelectedTierIndex(clamped, isCustom: false);

            Dictionary<string, string> convars = new(StringComparer.OrdinalIgnoreCase);
            foreach (ConvarSetting setting in Tiers[clamped].Tier.Values)
                convars[setting.Convar] = setting.Value;

            // No backup per change - these are individually reversible slider picks, not a whole
            // preset, same reasoning as GameplayTweakRow's per-toggle writes.
            if (configService.SetConvars(convars, createBackup: false))
                return;

            SetSelectedTierIndex(previousIndex, previousCustom);
        }
    }

    /// <summary>Whether no known tier currently matches - drives the "Custom" badge in the view.</summary>
    public bool IsCustom => _isCustom;

    /// <summary>
    /// The in-game screenshot for <see cref="SelectedTierIndex"/>, or <see langword="null"/> if no
    /// matching image has been added to <c>Assets/GraphicsPreviews/</c> yet - the view shows a
    /// plain placeholder either way.
    /// </summary>
    public IImage? PreviewImage => GraphicsPreviewImages.Get(previewId, Tiers[_selectedTierIndex].Tier.PreviewId);

    /// <summary>Whether <see cref="PreviewImage"/> resolved to a real image - drives the "no preview yet" placeholder in the view.</summary>
    public bool HasPreviewImage => PreviewImage is not null;

    /// <summary>Inverse of <see cref="HasPreviewImage"/>, exposed for the view's placeholder visibility binding.</summary>
    public bool HasNoPreviewImage => !HasPreviewImage;

    private void SetSelectedTierIndex(int index, bool isCustom)
    {
        _selectedTierIndex = index;
        _isCustom = isCustom;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTierIndex)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCustom)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewImage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasPreviewImage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNoPreviewImage)));
    }
}