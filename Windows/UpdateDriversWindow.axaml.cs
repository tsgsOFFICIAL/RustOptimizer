using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>
/// A window shell for the "Update Drivers" prompt: title bar, an explanation, one row per detected
/// component with a link to its vendor's driver page where one is known, and a Close footer.
/// </summary>
public partial class UpdateDriversWindow : Window
{
    [SupportedOSPlatform("windows")]
    public UpdateDriversWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static UpdateDriversViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new UpdateDriversViewModel(localization, new SystemInfoService(localization));
    }

    public UpdateDriversWindow(UpdateDriversViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += Close;
    }
}