using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Graphics page. All state lives in <see cref="GraphicsViewModel"/>; this class just
/// refreshes every slider's state from client.cfg on every visit rather than just the first, so a
/// change made outside the app (or a restored backup) is picked up.
/// </summary>
public partial class GraphicsView : UserControl
{
    /// <summary>Creates the view.</summary>
    public GraphicsView()
    {
        InitializeComponent();
    }

    /// <summary>Refreshes every slider's current tier from client.cfg.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as GraphicsViewModel)?.RefreshSliders();
    }
}