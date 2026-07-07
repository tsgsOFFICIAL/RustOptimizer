using IconPacks.Avalonia.PhosphorIcons;
using RustOptimizer.Interface;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls;
using System;

namespace RustOptimizer.Controls;

/// <summary>
/// The pages reachable from the sidebar nav rail. Only <see cref="Dashboard"/>, <see cref="Settings"/>
/// and <see cref="About"/> have real content so far - the rest are placeholders for future phases.
/// </summary>
public enum SidebarPage
{
    Dashboard,
    Optimizer,
    Graphics,
    System,
    Network,
    Gameplay,
    Configs,
    Utilities,
    BackupRestore,
    Settings,
    About
}

/// <summary>
/// The app's nav rail: brand header, page list with a single active selection, and a pinned
/// Rust status/launch section at the bottom.
/// </summary>
public partial class Sidebar : UserControl
{
    /// <summary>
    /// Raised when the user selects a different page from the nav list.
    /// </summary>
    public event EventHandler<SidebarPage>? NavigationRequested;

    /// <summary>
    /// Raised when the user clicks "Launch Rust".
    /// </summary>
    public event EventHandler? LaunchRustRequested;

    private Button[] NavButtons => new[]
    {
        NavDashboard, NavOptimizer, NavGraphics, NavSystem, NavNetwork,
        NavGameplay, NavConfigs, NavUtilities, NavBackupRestore, NavSettings, NavAbout
    };

    public Sidebar()
    {
        InitializeComponent();

        // The Dashboard button starts marked active in XAML; sync its icon to match rather than
        // hardcoding the filled kind there too, so this stays the one place that decides it.
        SetIconFilled(GetIcon(NavDashboard), filled: true);
    }

    private void OnNavItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } clicked || !Enum.TryParse(tag, out SidebarPage page))
            return;

        foreach (Button button in NavButtons)
        {
            bool isActive = button == clicked;
            button.Classes.Set("active", isActive);
            SetIconFilled(GetIcon(button), isActive);
        }

        NavigationRequested?.Invoke(this, page);
    }

    /// <summary>
    /// Gets the icon inside a nav button, which is always the first child of its content StackPanel.
    /// </summary>
    private static PackIconPhosphorIcons GetIcon(Button navButton)
        => (PackIconPhosphorIcons)((StackPanel)navButton.Content!).Children[0];

    /// <summary>
    /// Switches an icon between its outline and filled variant (e.g. "House" / "HouseFill") by name,
    /// so any icon works without a hardcoded outline-to-fill mapping - Phosphor Icons names every
    /// filled variant as the outline name plus a "Fill" suffix.
    /// </summary>
    private static void SetIconFilled(PackIconPhosphorIcons icon, bool filled)
    {
        string name = icon.Kind.ToString();
        string baseName = name.EndsWith("Fill") ? name[..^4] : name;
        string targetName = filled ? baseName + "Fill" : baseName;

        if (Enum.TryParse(targetName, out PackIconPhosphorIconsKind kind))
            icon.Kind = kind;
    }

    private void OnLaunchRustClick(object? sender, RoutedEventArgs e)
    {
        // No process is actually launched yet - this just reflects the click in the status
        // indicator so the mock UI has something to show for it.
        RustStatusDot.Fill = (IBrush)this.FindResource("SuccessColor")!;
        RustStatusText.Text = ((ILocalizationService)DataContext!)["RustRunning"];

        LaunchRustRequested?.Invoke(this, EventArgs.Empty);
    }
}