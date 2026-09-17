using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Keybinds page: every current key binding grouped by category, plus an "Add a bind" picker.
/// All state lives in <see cref="ViewModels.KeybindsViewModel"/>, resolved once at construction and
/// refreshed on every attach - no live polling happens on this page.
/// </summary>
public partial class KeybindsView : UserControl
{
    /// <summary>Creates the view.</summary>
    public KeybindsView()
    {
        InitializeComponent();
    }

    /// <summary>Re-reads keys.cfg, so changes made elsewhere (or restored via Backup &amp; Restore) show up on return.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as KeybindsViewModel)?.RefreshRows();
    }
}