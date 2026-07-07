using Avalonia.Interactivity;
using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// The main dashboard: hero banner, optimization overview, quick optimization presets, and a
/// system info / quick actions / preset profiles sidebar. Everything shown here is mock data for
/// now - wiring it to real system detection and optimization logic is a later phase.
/// </summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void OnRunSmartOptimizationClick(object? sender, RoutedEventArgs e)
    {
        // Mock data only for now - no real optimization logic wired up yet.
    }
}