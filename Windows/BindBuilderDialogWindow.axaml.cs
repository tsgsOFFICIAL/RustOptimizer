using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>The manual macro builder dialog shell. Closes with the built command string, or <see langword="null"/> if cancelled.</summary>
public partial class BindBuilderDialogWindow : Window
{
    [SupportedOSPlatform("windows")]
    public BindBuilderDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static BindBuilderDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new BindBuilderDialogViewModel(localization);
    }

    public BindBuilderDialogWindow(BindBuilderDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += command => Close(command);
    }
}