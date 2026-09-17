using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;

namespace RustOptimizer.Windows;

/// <summary>The "Choose from list" catalog dialog shell. Closes with the picked action's command string, or <see langword="null"/> if cancelled.</summary>
public partial class BindCatalogDialogWindow : Window
{
    [SupportedOSPlatform("windows")]
    public BindCatalogDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static BindCatalogDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new BindCatalogDialogViewModel(localization, []);
    }

    public BindCatalogDialogWindow(BindCatalogDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += command => Close(command);
    }

    /// <summary>Double-clicking a row confirms it immediately, same as clicking "Use This Action".</summary>
    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is BindCatalogDialogViewModel viewModel && sender is StyledElement { DataContext: CatalogActionRow row })
            viewModel.SelectCommand.Execute(row);
    }
}