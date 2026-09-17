using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using System.Collections.Generic;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One line within a <see cref="BindStageEditorRow"/> in the manual macro builder: a dropdown of
/// curated convars (<see cref="ConvarEditorCatalog"/>) plus the "Custom command" sentinel, and
/// whichever editor control matches the selected entry's <see cref="ConvarEditorKind"/> - a slider, a
/// toggle, a text field for <c>showtoast</c>'s message, or a free-text box for Custom. <see cref="RawText"/>
/// is always recomputed live from whichever control is active, and is the only thing
/// <see cref="KeybindCommandBuilder"/> ever reads - <see cref="SelectedConvar"/> exists purely so the
/// right editor control re-shows correctly, never consulted when assembling the final command.
/// </summary>
public sealed class BindLineEditorRow : ViewModelBase
{
    private ConvarPickerOption _selectedConvar;
    private double _sliderValue;
    private bool _toggleOn;
    private string _textValue = "";
    private string _customText = "";

    public BindLineEditorRow(ILocalizationService localization, IReadOnlyList<ConvarPickerOption> options) : base(localization)
    {
        Options = options;
        _selectedConvar = options[0];
    }

    /// <summary>Every convar the dropdown offers, plus the "Custom command" sentinel first.</summary>
    public IReadOnlyList<ConvarPickerOption> Options { get; }

    /// <summary>The convar (or Custom) currently selected for this line.</summary>
    public ConvarPickerOption SelectedConvar
    {
        get => _selectedConvar;
        set
        {
            if (!SetProperty(ref _selectedConvar, value))
                return;

            if (value.Entry is { Kind: ConvarEditorKind.Slider } entry)
                SliderValue = entry.Min;

            OnPropertyChanged(nameof(IsCustom));
            OnPropertyChanged(nameof(IsSlider));
            OnPropertyChanged(nameof(IsToggle));
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(SliderMin));
            OnPropertyChanged(nameof(SliderMax));
            OnPropertyChanged(nameof(SliderStep));
            OnPropertyChanged(nameof(RawText));
        }
    }

    /// <summary>Whether the "Custom command" sentinel is selected, showing the free-text field.</summary>
    public bool IsCustom => SelectedConvar.Entry is null;

    /// <summary>Whether the selected convar uses the slider editor.</summary>
    public bool IsSlider => SelectedConvar.Entry?.Kind == ConvarEditorKind.Slider;

    /// <summary>Whether the selected convar uses the on/off toggle editor.</summary>
    public bool IsToggle => SelectedConvar.Entry?.Kind == ConvarEditorKind.Toggle;

    /// <summary>Whether the selected convar uses the free-text message editor (<c>showtoast</c>).</summary>
    public bool IsText => SelectedConvar.Entry?.Kind == ConvarEditorKind.Text;

    /// <summary>The slider's minimum, meaningful only while <see cref="IsSlider"/> is true.</summary>
    public double SliderMin => SelectedConvar.Entry?.Min ?? 0;

    /// <summary>The slider's maximum, meaningful only while <see cref="IsSlider"/> is true.</summary>
    public double SliderMax => SelectedConvar.Entry?.Max ?? 1;

    /// <summary>The slider's step, meaningful only while <see cref="IsSlider"/> is true.</summary>
    public double SliderStep => SelectedConvar.Entry?.Step ?? 0.1;

    /// <summary>The slider's current value, meaningful only while <see cref="IsSlider"/> is true.</summary>
    public double SliderValue
    {
        get => _sliderValue;
        set { if (SetProperty(ref _sliderValue, value)) OnPropertyChanged(nameof(RawText)); }
    }

    /// <summary>The toggle's current state, meaningful only while <see cref="IsToggle"/> is true.</summary>
    public bool ToggleOn
    {
        get => _toggleOn;
        set { if (SetProperty(ref _toggleOn, value)) OnPropertyChanged(nameof(RawText)); }
    }

    /// <summary>The typed toast message, meaningful only while <see cref="IsText"/> is true. Never escaped beyond the surrounding quotes it's wrapped in.</summary>
    public string TextValue
    {
        get => _textValue;
        set { if (SetProperty(ref _textValue, value)) OnPropertyChanged(nameof(RawText)); }
    }

    /// <summary>The typed raw command, meaningful only while <see cref="IsCustom"/> is true. Never validated or sanitized beyond trimming.</summary>
    public string CustomText
    {
        get => _customText;
        set { if (SetProperty(ref _customText, value)) OnPropertyChanged(nameof(RawText)); }
    }

    /// <summary>The exact literal text this line contributes - recomputed live from whichever editor is active.</summary>
    public string RawText => SelectedConvar.Entry switch
    {
        null => CustomText.Trim(),
        { Kind: ConvarEditorKind.Slider } entry => $"{entry.ConvarKey} {SliderValue:0.##}",
        { Kind: ConvarEditorKind.Toggle } entry => $"{entry.ConvarKey} {(ToggleOn ? entry.ToggleOnValue : entry.ToggleOffValue)}",
        { Kind: ConvarEditorKind.Text } => $"showtoast 3 \"{TextValue}\"",
        _ => "",
    };

    /// <summary>Converts this row to the immutable <see cref="BindCommandLine"/> the builder consumes.</summary>
    public BindCommandLine ToLine() => new(SelectedConvar.Entry?.ConvarKey, RawText);
}