using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The main dashboard: hero banner, optimization overview, quick optimization presets, and a
/// system info / quick actions / preset profiles sidebar. All state lives in
/// <see cref="ViewModels.DashboardViewModel"/>, resolved once at construction and refreshed on
/// every attach - no live polling happens on this page.
/// </summary>
public partial class DashboardView : UserControl
{
    /// <summary>Creates the view.</summary>
    public DashboardView()
    {
        InitializeComponent();
    }

    /// <summary>Re-fetches the Optimization Overview's System/Network scores, so changes made on those pages show up on return.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as DashboardViewModel)?.RefreshSystemScore();
        (DataContext as DashboardViewModel)?.RefreshNetworkScore();
    }
}