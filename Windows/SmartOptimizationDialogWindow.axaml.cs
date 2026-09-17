using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for the Smart Optimization confirm prompt: title bar, the grouped change
/// list, and a Cancel/Apply footer - swapping to a brief progress state while applying. Closes with
/// the finished <see cref="SmartOptimizationOutcome"/>, or <see langword="null"/> if the user
/// cancelled before anything ran.
/// </summary>
public partial class SmartOptimizationDialogWindow : Window
{
    [SupportedOSPlatform("windows")]
    public SmartOptimizationDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static SmartOptimizationDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();

        RustProcessService rustProcess = new();
        ConfigBackupService configBackup = new(rustProcess);
        SmartOptimizationService smartOptimization = new(new SystemTweaksService(rustProcess), new NetworkTweaksService(),
            new ConfigService(rustProcess, configBackup), configBackup, new SystemInfoService(localization), rustProcess);

        return new SmartOptimizationDialogViewModel(localization, smartOptimization, smartOptimization.BuildPlan());
    }

    public SmartOptimizationDialogWindow(SmartOptimizationDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += outcome => Close(outcome);
    }
}