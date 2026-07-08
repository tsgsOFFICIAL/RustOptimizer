using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// Theme and language switching, presented as two segmented controls (sun/moon/system for theme,
/// native language names for language). All state lives in <c>SettingsViewModel</c>.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}