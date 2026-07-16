using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs a generic Yes/No confirmation prompt. <see cref="CloseRequested"/> carries the user's
/// choice so the hosting <c>ConfirmDialogWindow</c> can close itself with that result without this
/// view model referencing <c>Window</c> directly.
/// </summary>
public sealed class ConfirmDialogViewModel : ViewModelBase
{
    public ConfirmDialogViewModel(ILocalizationService localization, string title, string message, string confirmLabel, bool isDestructive)
        : base(localization)
    {
        Title = title;
        Message = message;
        ConfirmLabel = confirmLabel;
        IsDestructive = isDestructive;

        ConfirmCommand = new RelayCommand(() => CloseRequested?.Invoke(true));
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(false));
    }

    /// <summary>Raised once the user picks an option, carrying whether they confirmed.</summary>
    public event Action<bool>? CloseRequested;

    public string Title { get; }
    public string Message { get; }
    public string ConfirmLabel { get; }

    /// <summary>Whether the confirm button should render as a destructive (red) action.</summary>
    public bool IsDestructive { get; }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }
}