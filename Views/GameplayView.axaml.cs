using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>
/// The Gameplay page. All state lives in <see cref="GameplayViewModel"/>; this class just
/// refreshes each tweak's state from client.cfg on every visit rather than just the first, so a
/// change made outside the app (or a restored backup) is picked up.
/// </summary>
public partial class GameplayView : UserControl
{
    /// <summary>Creates the view.</summary>
    public GameplayView()
    {
        InitializeComponent();
    }

    /// <summary>Refreshes every tweak's current state from client.cfg.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as GameplayViewModel)?.RefreshTweaks();
    }
}