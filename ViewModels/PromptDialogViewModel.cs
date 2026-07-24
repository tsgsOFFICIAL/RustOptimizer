using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs a generic single-line text prompt (e.g. naming a profile). <see cref="CloseRequested"/>
/// carries the entered text, or <see langword="null"/> if the user cancelled, so the hosting
/// <c>PromptDialogWindow</c> can close itself with that result without this view model referencing
/// <c>Window</c> directly. Confirm stays disabled while the field is blank.
/// </summary>
public sealed class PromptDialogViewModel : ViewModelBase
{
    private string _value;

    /// <summary>Creates the view model with the prompt's text and the field's initial value.</summary>
    public PromptDialogViewModel(ILocalizationService localization, string title, string message, string confirmLabel, string initialValue)
        : base(localization)
    {
        Title = title;
        Message = message;
        ConfirmLabel = confirmLabel;
        _value = initialValue;

        ConfirmCommand = new RelayCommand(() => CloseRequested?.Invoke(Value.Trim()), () => !string.IsNullOrWhiteSpace(Value));
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(null));
    }

    /// <summary>Raised once the user picks an option, carrying the entered text or <see langword="null"/> on cancel.</summary>
    public event Action<string?>? CloseRequested;

    /// <summary>The prompt window's title.</summary>
    public string Title { get; }

    /// <summary>The explanatory line shown above the text field.</summary>
    public string Message { get; }

    /// <summary>The confirm button's label.</summary>
    public string ConfirmLabel { get; }

    /// <summary>The text currently entered in the field. Confirm is disabled while it's blank.</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Closes the prompt, returning the trimmed entered text.</summary>
    public RelayCommand ConfirmCommand { get; }

    /// <summary>Closes the prompt without a result.</summary>
    public RelayCommand CancelCommand { get; }
}