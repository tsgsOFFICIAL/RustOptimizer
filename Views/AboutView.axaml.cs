using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// App identity, a manual update check, and links out to the project's GitHub/Discord/Ko-fi.
/// All state lives in <c>AboutViewModel</c>.
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }
}