using RustOptimizer.ViewModels;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for a generic Yes/No confirmation prompt: title bar, message, and a
/// Cancel/confirm footer. Closes itself with the user's choice via
/// <see cref="ConfirmDialogViewModel.CloseRequested"/>.
/// </summary>
public partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>
    /// Creates an initialized view model for the Avalonia previewer.
    /// </summary>
    private static ConfirmDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new ConfirmDialogViewModel(localization, "Delete backup?", "This can't be undone.", "Delete", isDestructive: true);
    }

    public ConfirmDialogWindow(ConfirmDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += result => Close(result);
    }
}