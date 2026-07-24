using RustOptimizer.ViewModels;
using Avalonia.Interactivity;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia.Input;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for a single-line text prompt: title bar, a message, a text field, and a
/// Cancel/confirm footer. Focuses and selects the field on open and confirms on Enter, so naming
/// something is a type-and-press interaction. Closes with the entered text via
/// <see cref="PromptDialogViewModel.CloseRequested"/>.
/// </summary>
public partial class PromptDialogWindow : Window
{
    /// <summary>Creates the window with a design-time view model for the Avalonia previewer.</summary>
    public PromptDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    private static PromptDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new PromptDialogViewModel(localization, "Save profile as", "Name this profile.", "Save", "My Profile");
    }

    /// <summary>Creates the window for the given view model and closes with the user's entered text.</summary>
    public PromptDialogWindow(PromptDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += result => Close(result);
    }

    /// <summary>Focuses and selects the text field once the window opens, so the user can type straight away.</summary>
    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        if (this.FindControl<TextBox>("ValueBox") is { } box)
        {
            box.Focus();
            box.SelectAll();
        }
    }

    /// <summary>Confirms on Enter, cancels on Escape, so the prompt can be completed or dismissed from the keyboard alone.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PromptDialogViewModel viewModel)
            return;

        if (e.Key == Key.Enter && viewModel.ConfirmCommand.CanExecute(null))
        {
            viewModel.ConfirmCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            viewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>Wires the window-level key handler used for Enter-to-confirm and Escape-to-cancel.</summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        KeyDown += OnKeyDown;
    }
}