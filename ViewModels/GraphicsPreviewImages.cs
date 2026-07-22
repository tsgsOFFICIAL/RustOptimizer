using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Media;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Resolves and caches the in-game preview screenshot for a graphics slider's tier, if one has
/// been added to <c>Assets/GraphicsPreviews/</c> - see that folder's README for the naming
/// convention. Missing images are expected and resolve to <see langword="null"/> rather than throwing, so the Graphics page can show
/// a plain "no preview yet" placeholder instead of crashing.
/// </summary>
internal static class GraphicsPreviewImages
{
    private static readonly string[] SupportedExtensions = ["png", "jpg"];
    private static readonly ConcurrentDictionary<string, IImage?> Cache = new();

    /// <summary>
    /// The preview image for <paramref name="sliderPreviewId"/>/<paramref name="tierPreviewId"/>
    /// (e.g. "ShadowQuality"/"Low"), or <see langword="null"/> if no matching image has been added yet.
    /// </summary>
    public static IImage? Get(string sliderPreviewId, string tierPreviewId) =>
        Cache.GetOrAdd($"{sliderPreviewId}_{tierPreviewId}", Load);

    private static IImage? Load(string fileNameStem)
    {
        foreach (string extension in SupportedExtensions)
        {
            Uri uri = new($"avares://RustOptimizer/Assets/GraphicsPreviews/{fileNameStem}.{extension}");
            if (!AssetLoader.Exists(uri))
                continue;

            using System.IO.Stream stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }

        return null;
    }
}