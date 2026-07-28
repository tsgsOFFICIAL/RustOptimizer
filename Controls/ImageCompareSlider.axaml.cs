using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia;
using System;

namespace RustOptimizer.Controls;

/// <summary>
/// A before/after image reveal: <see cref="AfterContent"/> fills the frame, <see cref="BeforeContent"/>
/// sits on top clipped to <see cref="Position"/>, and dragging anywhere in the frame - not just the
/// handle - moves the divider. Used by the Graphics page's preview lightbox so a slider's Low and
/// High tier screenshots can be compared directly instead of switching the tier back and forth.
/// </summary>
public partial class ImageCompareSlider : UserControl
{
    public static readonly StyledProperty<object?> BeforeContentProperty =
        AvaloniaProperty.Register<ImageCompareSlider, object?>(nameof(BeforeContent));

    public static readonly StyledProperty<object?> AfterContentProperty =
        AvaloniaProperty.Register<ImageCompareSlider, object?>(nameof(AfterContent));

    public static readonly StyledProperty<string?> BeforeLabelProperty =
        AvaloniaProperty.Register<ImageCompareSlider, string?>(nameof(BeforeLabel));

    public static readonly StyledProperty<string?> AfterLabelProperty =
        AvaloniaProperty.Register<ImageCompareSlider, string?>(nameof(AfterLabel));

    /// <summary>Divider position as a 0 ("after" only) to 1 ("before" only) fraction of the frame's width.</summary>
    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<ImageCompareSlider, double>(nameof(Position), defaultValue: 0.5);

    private bool _dragging;

    public object? BeforeContent
    {
        get => GetValue(BeforeContentProperty);
        set => SetValue(BeforeContentProperty, value);
    }

    public object? AfterContent
    {
        get => GetValue(AfterContentProperty);
        set => SetValue(AfterContentProperty, value);
    }

    public string? BeforeLabel
    {
        get => GetValue(BeforeLabelProperty);
        set => SetValue(BeforeLabelProperty, value);
    }

    public string? AfterLabel
    {
        get => GetValue(AfterLabelProperty);
        set => SetValue(AfterLabelProperty, value);
    }

    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, Math.Clamp(value, 0, 1));
    }

    public ImageCompareSlider()
    {
        InitializeComponent();

        // Bounds-driven rather than fixed: the lightbox can be resized with the window, and the
        // clip/handle need to track the frame's actual size, not whatever it was at construction.
        SizeChanged += (_, _) => UpdateHandlePosition();
        UpdateHandlePosition();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
            UpdateHandlePosition();
    }

    private void UpdateHandlePosition()
    {
        double width = Bounds.Width;
        double height = Bounds.Height;
        double x = width * Position;

        // A fresh Geometry instance every time, not a mutated Rect on a reused one: Visual.Clip
        // only re-clips reliably when the property itself changes, not when a sub-property of the
        // existing Geometry object mutates - the Divider/Handle margins are plain layout properties
        // and always re-rendered fine, which is what made this so easy to miss.
        BeforePresenter.Clip = new RectangleGeometry(new Rect(0, 0, x, height));
        Divider.Margin = new Thickness(x - Divider.Width / 2, 0, 0, 0);
        Handle.Margin = new Thickness(x - Handle.Width / 2, 0, 0, 0);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        e.Pointer.Capture(RootGrid);
        UpdatePositionFromPointer(e.GetPosition(RootGrid));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging)
            UpdatePositionFromPointer(e.GetPosition(RootGrid));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }

    private void UpdatePositionFromPointer(Point point)
    {
        if (Bounds.Width > 0)
            Position = point.X / Bounds.Width;
    }
}