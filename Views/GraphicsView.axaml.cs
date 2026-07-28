using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Graphics page. All state lives in <see cref="GraphicsViewModel"/>; this class just
/// refreshes every slider's state from client.cfg on every visit rather than just the first, so a
/// change made outside the app (or a restored backup) is picked up. It also wires Escape-to-close
/// for the preview lightbox, which the view model can't do itself - closing on a key press is a
/// view concern, and the lightbox is a page-level overlay rather than its own focus scope.
/// </summary>
public partial class GraphicsView : UserControl
{
    private TopLevel? _topLevel;

    /// <summary>Creates the view.</summary>
    public GraphicsView()
    {
        InitializeComponent();
    }

    /// <summary>Refreshes every slider's current tier from client.cfg, and starts listening for Escape.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as GraphicsViewModel)?.RefreshSliders();

        // Attached at the TopLevel with Tunnel, not on this control with Bubble: nothing on this
        // page normally holds keyboard focus, so a Bubble handler here would only ever see a key
        // press if focus already happened to be somewhere inside GraphicsView.
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    /// <summary>Stops listening once the page is navigated away from, so the handler doesn't outlive the view.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Closes the preview lightbox on Escape. Left unhandled - and so free to close anything
    /// else that also reacts to Escape - whenever the lightbox isn't the thing open.</summary>
    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (DataContext is GraphicsViewModel { ActiveLightbox: { } lightbox })
        {
            lightbox.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>Closes the preview lightbox when the scrim behind the card is clicked.</summary>
    private void OnScrimPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is GraphicsViewModel { ActiveLightbox: { } lightbox })
            lightbox.CloseCommand.Execute(null);
    }
}