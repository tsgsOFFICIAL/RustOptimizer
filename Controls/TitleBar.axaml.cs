using IconPacks.Avalonia.Material;
using Avalonia.Interactivity;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System;

namespace RustOptimizer.Controls;

/// <summary>
/// A reusable custom title bar that replaces the native window chrome with a themed header
/// containing an icon, title, and window control buttons. Only takes effect on Windows
/// (matching the borderless-window pattern of the WPF apps this was ported from); on other
/// platforms it stays hidden and the native OS title bar/decorations are used instead, since
/// fully removing decorations there loses window-manager behavior (traffic lights, snapping,
/// accessibility) that Avalonia doesn't yet provide a mature managed replacement for.
/// </summary>
public partial class TitleBar : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Title));

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowMaximizeButtonProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(ShowMaximizeButton), defaultValue: true);

    public static readonly StyledProperty<string?> AuthorProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Author));

    public static readonly StyledProperty<string?> VersionTextProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(VersionText), defaultValue: GetDefaultVersionText());

    private Window? _window;

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    /// <summary>
    /// The attribution text shown under the title as "by {Author}". Left unset (null), nothing is shown.
    /// </summary>
    public string? Author
    {
        get => GetValue(AuthorProperty);
        set => SetValue(AuthorProperty, value);
    }

    /// <summary>
    /// The version text shown next to the title. Defaults to the entry assembly's version.
    /// </summary>
    public string? VersionText
    {
        get => GetValue(VersionTextProperty);
        set => SetValue(VersionTextProperty, value);
    }

    private static string? GetDefaultVersionText()
    {
        // GetEntryAssembly() can resolve to a hosting/diagnostics assembly (e.g. Avalonia's own
        // version) under some debug hosts, so read the version of this assembly directly instead.
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? null : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    public TitleBar()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is null)
            return;

        if (!OperatingSystem.IsWindows())
        {
            IsVisible = false;
            return;
        }

        // BorderOnly (not None) keeps the native resize border/hit-testing at the window edges
        // while still dropping the native title bar and caption buttons. Extending the client
        // area into the decorations (with a height hint of 0, not -1/"default") tells Windows
        // there is no title-bar strip to reserve, which removes the leftover 1px accent line
        // it otherwise draws along the top of a BorderOnly window.
        _window.WindowDecorations = WindowDecorations.BorderOnly;
        _window.ExtendClientAreaToDecorationsHint = true;
        _window.ExtendClientAreaTitleBarHeightHint = 0;

        UpdateMaximizeRestoreIcon(_window.WindowState);
        _window.PropertyChanged += OnWindowPropertyChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_window is not null)
            _window.PropertyChanged -= OnWindowPropertyChanged;

        _window = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
            UpdateMaximizeRestoreIcon((WindowState)e.NewValue!);
    }

    private void UpdateMaximizeRestoreIcon(WindowState state)
        => MaximizeRestoreIcon.Kind = state == WindowState.Maximized
            ? PackIconMaterialKind.WindowRestore
            : PackIconMaterialKind.WindowMaximize;

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window is null)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        _window.BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (_window is not null)
            _window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object? sender, RoutedEventArgs e)
        => ToggleMaximizeRestore();

    private void OnCloseClick(object? sender, RoutedEventArgs e)
        => _window?.Close();

    private void ToggleMaximizeRestore()
    {
        if (_window is null)
            return;

        _window.WindowState = _window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}