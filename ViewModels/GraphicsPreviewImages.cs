using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Builds the in-game preview control for a graphics slider's tier, if a matching image has been
/// added to <c>Assets/GraphicsPreviews/</c> - see that folder's README for the naming convention.
/// Missing/corrupt images are expected and resolve to <see langword="null"/> rather
/// than throwing, so the Graphics page can show a plain "no preview yet" placeholder instead of
/// crashing.
/// </summary>
internal static class GraphicsPreviewImages
{
    private static readonly string[] SupportedExtensions = ["png", "jpg", "gif"];

    // Caches the decoded bytes, not the built Control - an Avalonia Control can only live in one
    // place in the visual tree at a time (and an animated one owns its own timer/codec), so every
    // call still needs its own fresh Control even when the underlying image was already read once.
    private static readonly ConcurrentDictionary<string, byte[]?> ByteCache = new();

    /// <summary>
    /// A freshly built preview control for <paramref name="sliderPreviewId"/>/<paramref name="tierPreviewId"/>
    /// (e.g. "ShadowQuality"/"Low"), or <see langword="null"/> if no matching image has been added yet.
    /// </summary>
    public static Control? Build(string sliderPreviewId, string tierPreviewId)
    {
        byte[]? bytes = ByteCache.GetOrAdd($"{sliderPreviewId}_{tierPreviewId}", LoadBytes);
        if (bytes is null)
            return null;

        try
        {
            Control control = AnimatedImage.BuildImageControl(bytes);

            // BuildImageControl sizes/aligns itself for the Changelog viewer's layout - override to
            // fill this page's fixed-size preview frame edge-to-edge instead.
            if (control is Image image)
            {
                image.Stretch = Stretch.UniformToFill;
                image.HorizontalAlignment = HorizontalAlignment.Stretch;
                image.VerticalAlignment = VerticalAlignment.Stretch;
                image.MaxWidth = double.PositiveInfinity;
                image.MaxHeight = double.PositiveInfinity;
            }

            return control;
        }
        catch
        {
            // Unrecognized/corrupt image data - show the "no preview" placeholder instead of crashing.
            return null;
        }
    }

    private static byte[]? LoadBytes(string fileNameStem)
    {
        foreach (string extension in SupportedExtensions)
        {
            Uri uri = new($"avares://RustOptimizer/Assets/GraphicsPreviews/{fileNameStem}.{extension}");
            if (!AssetLoader.Exists(uri))
                continue;

            using Stream assetStream = AssetLoader.Open(uri);
            using MemoryStream buffer = new();
            assetStream.CopyTo(buffer);
            return buffer.ToArray();
        }

        return null;
    }
}