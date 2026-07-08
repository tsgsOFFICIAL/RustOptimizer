using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// A reusable placeholder shown for sidebar pages that don't have real content yet. A single
/// instance is reused across all of them (see <c>MainWindowViewModel</c>), with the title swapped
/// per page via <c>ComingSoonViewModel.Title</c>.
/// </summary>
public partial class ComingSoonView : UserControl
{
    public ComingSoonView()
    {
        InitializeComponent();
    }
}