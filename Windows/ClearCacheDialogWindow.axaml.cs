using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for the Clear Cache prompt: title bar, a summary of what gets cleared, the
/// three opt-out toggles, and a Cancel/Clear footer - swapping to a progress bar while the run is in
/// progress. Closes with the finished <see cref="CleanupOutcome"/>, or <see langword="null"/> if the
/// user cancelled before anything started.
/// </summary>
public partial class ClearCacheDialogWindow : Window
{
    [SupportedOSPlatform("windows")]
    public ClearCacheDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static ClearCacheDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new ClearCacheDialogViewModel(localization, new CleanupService(new RustProcessService()));
    }

    public ClearCacheDialogWindow(ClearCacheDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += outcome => Close(outcome);
    }
}