using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for the key-capture dialog: a visual keyboard/mouse picker with live free/used
/// status, and a Cancel footer. Physically pressing a keyboard key has the same effect as clicking its
/// on-screen chip, routed via <see cref="KeyCaptureDialogViewModel.HandleModifierKey"/>/
/// <see cref="KeyCaptureDialogViewModel.HandleKeyToken"/>. Mouse buttons are deliberately NOT captured
/// this way - a physical mouse click and a click on this window's own UI (the Cancel/Use Anyway
/// buttons, a key chip) are the same hardware event, so a window-level "physical mouse press"
/// handler can't tell them apart; it previously intercepted every click as "mouse0 pressed" before
/// the real target ever saw it, silently breaking every button in this dialog. Every mouse button and
/// wheel direction already has its own correctly-wired chip in the on-screen Mouse panel, so nothing
/// is lost by not also capturing raw pointer presses. Closes with the chosen <see cref="KeyToken"/>,
/// or <see langword="null"/> if the user cancelled.
/// </summary>
public partial class KeyCaptureDialogWindow : Window
{
    [SupportedOSPlatform("windows")]
    public KeyCaptureDialogWindow() : this(CreateDesignViewModel()) { }

    /// <summary>Creates an initialized view model for the Avalonia previewer.</summary>
    [SupportedOSPlatform("windows")]
    private static KeyCaptureDialogViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();
        return new KeyCaptureDialogViewModel(localization, [], excludeToken: null);
    }

    public KeyCaptureDialogWindow(KeyCaptureDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += token => Close(token);

        AddHandler(KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        Opened += (_, _) => Focus();
    }

    /// <summary>Routes a physical key press to the view model as either a modifier or a finalizing key, then stops it from reaching any child control.</summary>
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not KeyCaptureDialogViewModel viewModel)
            return;

        if (e.Key == Key.Escape)
            return;

        // Not while a conflict prompt (or anything else modal-ish within the dialog) is up - a
        // physical key press here should never silently re-arm key selection underneath it.
        if (viewModel.IsConfirmingConflict)
            return;

        if (KeyTokenCatalog.ModifierFromAvaloniaKey(e.Key) is { } modifierToken)
        {
            viewModel.HandleModifierKey(modifierToken);
            e.Handled = true;
            return;
        }

        if (KeyTokenCatalog.FromAvaloniaKey(e.Key) is { } keyToken)
        {
            viewModel.HandleKeyToken(keyToken);
            e.Handled = true;
        }
    }
}