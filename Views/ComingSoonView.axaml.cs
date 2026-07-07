using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// A reusable placeholder shown for sidebar pages that don't have real content yet. A single
/// instance is reused across all of them (see MainWindow), with the title swapped per page.
/// </summary>
public partial class ComingSoonView : UserControl
{
    public ComingSoonView()
    {
        InitializeComponent();
    }

    public void SetTitle(string title) => TitleText.Text = title;
}