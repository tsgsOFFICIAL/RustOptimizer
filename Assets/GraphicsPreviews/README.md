# Graphics slider previews

Drop screenshots here and they'll show up as the preview next to each slider on the Graphics page.
They get picked up automatically on the next build, since `Assets\**` is already globbed as an
`AvaloniaResource` in `RustOptimizer.csproj`, so there's nothing else to hook up.

## Naming convention

Name files `{SliderPreviewId}_{TierPreviewId}.png`, like `ShadowQuality_Low.png`. `.jpg` and `.gif`
work fine too, e.g. `WaterQuality_High.gif`.

If a slider's missing a file for whatever tier is selected, it just falls back to a "no preview
available" placeholder. Nothing breaks.

Multi-frame `.gif`s animate on their own, using the same SkiaSharp decoder the Changelog viewer's
images use (`AnimatedImage.BuildImageControl` in `AnimatedImage.cs`), so a before/after clip works
just as well as a plain screenshot.

| Slider (title shown in-app) | `SliderPreviewId`  |
|-----------------------------|--------------------|
| Shadow Quality              | `ShadowQuality`    |
| Texture Quality             | `TextureQuality`   |
| Effects Quality             | `EffectsQuality`   |
| Draw Distance               | `DrawDistance`     |
| World Detail                | `WorldDetail`      |
| Water Quality               | `WaterQuality`     |
| Inventory Display           | `InventoryDisplay` |

Tiers are always `Low`, `Medium`, or `High`.

Which means the full list of filenames (21 in total) looks like this:

```
ShadowQuality_Low.png       ShadowQuality_Medium.png       ShadowQuality_High.png
TextureQuality_Low.png      TextureQuality_Medium.png      TextureQuality_High.png
EffectsQuality_Low.png      EffectsQuality_Medium.png      EffectsQuality_High.png
DrawDistance_Low.png        DrawDistance_Medium.png        DrawDistance_High.png
WorldDetail_Low.png         WorldDetail_Medium.png         WorldDetail_High.png
WaterQuality_Low.png        WaterQuality_Medium.png        WaterQuality_High.png
InventoryDisplay_Low.png    InventoryDisplay_Medium.png    InventoryDisplay_High.png
```

These IDs come straight from `Service/RecommendedGraphicsSliders.cs` and the
`GraphicsSlider`/`GraphicsSliderTier` records in `Interface/IConfigService.cs`. If a slider or tier
changes there, update this table too.

Best to shoot each pair (like `WaterQuality_Low` vs `WaterQuality_High`) from the same spot, same
time of day and weather, so what changes in the preview is the setting itself and not just the
scenery.