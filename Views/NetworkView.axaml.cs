using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Network page. All state lives in <see cref="NetworkViewModel"/>; this class just re-reads
/// tweaks from the registry on every visit (since they can change outside the app) and forwards
/// visual-tree lifecycle events so live adapter-info polling pauses while a different page shows.
/// </summary>
public partial class NetworkView : UserControl
{
    /// <summary>Creates the view.</summary>
    public NetworkView()
    {
        InitializeComponent();
    }

    /// <summary>Refreshes tweaks from the registry and starts the view model's live adapter-info polling.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as NetworkViewModel)?.RefreshTweaks();
        (DataContext as NetworkViewModel)?.StartPolling();
    }

    /// <summary>Stops the view model's live adapter-info polling.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (DataContext as NetworkViewModel)?.StopPolling();
    }
}