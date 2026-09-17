using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Optimizer page: the Smart Optimization action plus a full, untruncated view of every
/// category's outstanding checks. All state lives in <see cref="ViewModels.OptimizerViewModel"/>,
/// resolved once at construction and refreshed on every attach - no live polling happens on this page.
/// </summary>
public partial class OptimizerView : UserControl
{
    /// <summary>Creates the view.</summary>
    public OptimizerView()
    {
        InitializeComponent();
    }

    /// <summary>Re-fetches every category's state, so changes made on those pages show up on return.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as OptimizerViewModel)?.RefreshAll();
    }
}