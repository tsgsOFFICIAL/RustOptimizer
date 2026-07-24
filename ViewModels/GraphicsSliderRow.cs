using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using Avalonia.Controls;
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
/// and rebuilds <see cref="PreviewControl"/> to match.
/// </summary>
public sealed class GraphicsSliderRow(string title, string previewId, IReadOnlyList<GraphicsTierOption> tiers, IConfigService configService, int? currentTierIndex) : INotifyPropertyChanged
{
    private int _selectedTierIndex = currentTierIndex ?? 0;
    private bool _isCustom = currentTierIndex is null;

    // Built once per selection rather than recomputed on every property read - a multi-frame GIF's
    // control owns a live DispatcherTimer/codec, so re-reading this property shouldn't spin up a
    // fresh one each time something merely re-evaluates the binding.
    private Control? _previewControl = GraphicsPreviewImages.Build(previewId, tiers[currentTierIndex ?? 0].Tier.PreviewId);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The slider's display name (e.g. "Shadow Quality").</summary>
    public string Title { get; } = title;

    /// <summary>
    /// The slider's stable, language-independent identifier (e.g. "ShadowQuality"), used as the key
    /// when a graphics profile records which tier this slider is set to.
    /// </summary>
    public string PreviewId => previewId;

    /// <summary>Every tier this slider can be set to, in display order (Low/Medium/High) - three per slider.</summary>
    public IReadOnlyList<GraphicsTierOption> Tiers { get; } = tiers;

    /// <summary>
    /// The stable identifier of the tier the slider handle currently sits on (e.g. "High"). Reflects
    /// the handle position regardless of <see cref="IsCustom"/> - a profile records where the handle
    /// is, while <see cref="IsCustom"/> separately says whether client.cfg actually matches it.
    /// </summary>
    public string SelectedTierPreviewId => Tiers[_selectedTierIndex].Tier.PreviewId;

    /// <summary>
    /// Whether the named tier writes the same convar values as the tier the handle currently sits on.
    /// Some sliders have duplicate tiers (shadows and water share identical Low and Medium values), so
    /// a profile's stored "Medium" and a resolved "Low" describe the very same client.cfg and must
    /// count as equal - otherwise selecting such a profile reads as "unsaved changes" the instant it's applied.
    /// </summary>
    public bool CurrentTierValuesMatch(string tierPreviewId)
    {
        GraphicsSliderTier current = Tiers[_selectedTierIndex].Tier;
        if (string.Equals(current.PreviewId, tierPreviewId, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (GraphicsTierOption option in Tiers)
            if (string.Equals(option.Tier.PreviewId, tierPreviewId, StringComparison.OrdinalIgnoreCase))
                return ConvarsEqual(current.Values, option.Tier.Values);

        return false;
    }

    /// <summary>Whether two tiers' convar name/value sets are identical, order-independent.</summary>
    private static bool ConvarsEqual(IReadOnlyList<ConvarSetting> a, IReadOnlyList<ConvarSetting> b)
    {
        if (a.Count != b.Count)
            return false;

        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (ConvarSetting setting in a)
            map[setting.Convar] = setting.Value;

        foreach (ConvarSetting setting in b)
            if (!map.TryGetValue(setting.Convar, out string? value) || !string.Equals(value, setting.Value, StringComparison.OrdinalIgnoreCase))
                return false;

        return true;
    }

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
    /// The in-game preview control for <see cref="SelectedTierIndex"/> (animates if it's a
    /// multi-frame GIF), or <see langword="null"/> if no matching image has been added to
    /// <c>Assets/GraphicsPreviews/</c> yet - the view shows a plain placeholder either way.
    /// </summary>
    public Control? PreviewControl => _previewControl;

    /// <summary>Whether <see cref="PreviewControl"/> resolved to a real image - drives the "no preview yet" placeholder in the view.</summary>
    public bool HasPreviewImage => _previewControl is not null;

    /// <summary>Inverse of <see cref="HasPreviewImage"/>, exposed for the view's placeholder visibility binding.</summary>
    public bool HasNoPreviewImage => !HasPreviewImage;

    private void SetSelectedTierIndex(int index, bool isCustom)
    {
        _selectedTierIndex = index;
        _isCustom = isCustom;
        _previewControl = GraphicsPreviewImages.Build(previewId, Tiers[index].Tier.PreviewId);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTierIndex)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCustom)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewControl)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasPreviewImage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNoPreviewImage)));
    }
}
