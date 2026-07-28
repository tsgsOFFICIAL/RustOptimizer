using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using Avalonia.Controls;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the Graphics page's preview lightbox: an enlarged view of a slider's Low and High tier
/// screenshots with a draggable divider between them (see <see cref="Controls.ImageCompareSlider"/>).
/// Built fresh each time a thumbnail is clicked rather than reused, since the underlying
/// <see cref="Control"/>s can only live in one place in the visual tree at a time - the inline
/// thumbnail keeps its own copy, decoded separately by <see cref="GraphicsPreviewImages"/>.
/// </summary>
public sealed class GraphicsPreviewLightboxViewModel : ViewModelBase
{
    public GraphicsPreviewLightboxViewModel(ILocalizationService localization, string title,
        Control? lowImage, Control? highImage, string lowLabel, string highLabel)
        : base(localization)
    {
        Title = title;
        LowImage = lowImage;
        HighImage = highImage;
        LowLabel = lowLabel;
        HighLabel = highLabel;
        CompareHint = localization["GraphicsPreviewCompareHint"];

        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
    }

    /// <summary>Raised when the user dismisses the lightbox - closes the button, the scrim, or Escape.</summary>
    public event Action? CloseRequested;

    /// <summary>The slider's title (e.g. "Shadow Quality"), shown as the lightbox's heading.</summary>
    public string Title { get; }

    /// <summary>The Low tier's decoded preview image, or <see langword="null"/> if none has been added yet.</summary>
    public Control? LowImage { get; }

    /// <summary>The High tier's decoded preview image, or <see langword="null"/> if none has been added yet.</summary>
    public Control? HighImage { get; }

    public string LowLabel { get; }
    public string HighLabel { get; }

    /// <summary>Resolved hint text explaining the drag-to-compare gesture.</summary>
    public string CompareHint { get; }

    public RelayCommand CloseCommand { get; }
}