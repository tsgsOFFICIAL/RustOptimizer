using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the reusable placeholder shown for sidebar pages that don't have real content yet.
/// </summary>
public sealed class ComingSoonViewModel(ILocalizationService localization) : ViewModelBase(localization)
{
    private string _title = "";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}