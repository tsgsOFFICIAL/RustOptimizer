using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The main dashboard: hero banner, optimization overview, quick optimization presets, and a
/// system info / quick actions / preset profiles sidebar. All state lives in
/// <see cref="DashboardViewModel"/>; this class just forwards visual-tree lifecycle events so the
/// live system-info poll pauses while the Dashboard page isn't visible.
/// </summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as DashboardViewModel)?.StartPolling();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        (DataContext as DashboardViewModel)?.StopPolling();
    }
}