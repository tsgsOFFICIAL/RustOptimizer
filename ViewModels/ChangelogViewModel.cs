using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the changelog viewer's content. <see cref="CloseRequested"/> lets the hosting
/// <c>ChangelogWindow</c> close itself without this view model referencing <c>Window</c> directly.
/// </summary>
public sealed class ChangelogViewModel : ViewModelBase
{
    public ChangelogViewModel(ILocalizationService localization, string markdown) : base(localization)
    {
        Markdown = markdown;
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
    }

    public event Action? CloseRequested;

    public string Markdown { get; }

    public RelayCommand CloseCommand { get; }
}