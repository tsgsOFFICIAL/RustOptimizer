using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The System page. All state lives in <see cref="SystemViewModel"/>; this class just forwards
/// visual-tree lifecycle events so the live usage poll pauses while the page isn't visible, and
/// gaming tweaks are re-read from the registry on every visit rather than just the first.
/// </summary>
public partial class SystemView : UserControl
{
    /// <summary>Creates the view.</summary>
    public SystemView()
    {
        InitializeComponent();
    }

    /// <summary>Starts the view model's live-usage polling and refreshes gaming tweaks from the registry.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as SystemViewModel)?.StartPolling();
        (DataContext as SystemViewModel)?.RefreshGamingTweaks();
    }

    /// <summary>Stops the view model's live-usage polling.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (DataContext as SystemViewModel)?.StopPolling();
    }
}