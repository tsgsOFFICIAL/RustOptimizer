using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;

namespace RustOptimizer.Windows;

/// <summary>
/// A small window shell for the key-capture dialog: a modifier picker, a scrollable list of every
/// known key with live free/used status, and a Cancel footer. Physically pressing a key or mouse
/// button/wheel has the same effect as clicking its row - both go through
/// <see cref="KeyCaptureDialogViewModel.HandleModifierKey"/>/<see cref="KeyCaptureDialogViewModel.HandleKeyToken"/>.
/// Input is captured at the tunnelling (preview) stage on the window itself so a focused child
/// control (e.g. the modifier ComboBox) never swallows a key press meant to be captured. Closes with
/// the chosen <see cref="KeyToken"/>, or <see langword="null"/> if the user cancelled.
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
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        Opened += (_, _) => Focus();
    }

    /// <summary>Routes a physical key press to the view model as either a modifier or a finalizing key, then stops it from reaching any child control.</summary>
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not KeyCaptureDialogViewModel viewModel)
            return;

        if (e.Key == Key.Escape)
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

    /// <summary>Routes a physical mouse button press to the view model, same as a key press.</summary>
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not KeyCaptureDialogViewModel viewModel)
            return;

        if (KeyTokenCatalog.FromPointerUpdateKind(e.GetCurrentPoint(this).Properties.PointerUpdateKind) is { } token)
        {
            viewModel.HandleKeyToken(token);
            e.Handled = true;
        }
    }

    /// <summary>Routes a mouse wheel scroll to the view model as mousewheelup/mousewheeldown.</summary>
    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not KeyCaptureDialogViewModel viewModel)
            return;

        viewModel.HandleKeyToken(e.Delta.Y > 0 ? "mousewheelup" : "mousewheeldown");
        e.Handled = true;
    }
}