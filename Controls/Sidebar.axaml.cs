using IconPacks.Avalonia.PhosphorIcons;
using RustOptimizer.ViewModels;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia;
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
/// Rust status/launch section at the bottom. All state lives in <see cref="SidebarViewModel"/>,
/// exposed via <see cref="ViewModel"/> - a dedicated property bound explicitly (like
/// <c>TitleBar.Localization</c>) rather than relying on the ambient
/// <see cref="StyledElement.DataContext"/>. Avalonia evaluates a control's compiled bindings as
/// soon as it's attached to the visual tree, which happens before a parent's own
/// "DataContext={Binding ...}" override (or any code-behind assignment after InitializeComponent)
/// has a chance to land - in that gap this control would inherit its parent's DataContext (a
/// different view model type) and the compiled binding's hard cast would throw. A dedicated
/// StyledProperty has no such gap: it starts at a harmless null and only ever becomes the correct
/// type.
/// </summary>
public partial class Sidebar : UserControl
{
    public static readonly StyledProperty<SidebarViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<Sidebar, SidebarViewModel?>(nameof(ViewModel));

    public Sidebar()
    {
        InitializeComponent();
    }

    public SidebarViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ViewModelProperty)
            return;

        if (change.OldValue is SidebarViewModel oldViewModel)
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (change.NewValue is SidebarViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateActiveIcon();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ViewModel?.StartPolling();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ViewModel?.StopPolling();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.ActivePage))
            UpdateActiveIcon();
    }

    private Button[] NavButtons =>
    [
        NavDashboard, NavOptimizer, NavGraphics, NavSystem, NavNetwork,
        NavGameplay, NavConfigs, NavUtilities, NavBackupRestore, NavSettings, NavAbout
    ];

    /// <summary>
    /// Switches every nav button's icon between its outline and filled variant to match whichever
    /// one is <see cref="SidebarViewModel.ActivePage"/>.
    /// </summary>
    private void UpdateActiveIcon()
    {
        if (ViewModel is not { } viewModel)
            return;

        string activeTag = viewModel.ActivePage.ToString();

        foreach (Button button in NavButtons)
            SetIconFilled(GetIcon(button), filled: (string?)button.Tag == activeTag);
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
}