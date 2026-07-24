using System.Collections.Generic;
using RustOptimizer.Interface;

namespace RustOptimizer.Service;

/// <summary>
/// Simplified quality sliders for the Graphics page - Rust-menu-style groupings (Shadow Quality,
/// Texture Quality, Water Quality, etc.) over the same convars <see cref="RustConfigPresets"/>
/// already sets per preset. Each tier's values are exactly what <see cref="ConfigPreset.LowEndPc"/>/
/// <see cref="ConfigPreset.Competitive"/>/<see cref="ConfigPreset.Cinematic"/> already use for that
/// convar - "Low"/"Medium"/"High" are a friendlier relabeling of tiers the app already recommends
/// elsewhere, not independently invented values. <see cref="RustConfigPresets"/>'s shared "Common"
/// convars aren't covered here since they don't vary between presets - there's no tier to pick.
/// Each slider/tier's <c>PreviewId</c> pair names the in-game screenshot shown for it - see
/// <c>Assets/GraphicsPreviews/README.md</c>.
/// </summary>
internal static class RecommendedGraphicsSliders
{
    public static IReadOnlyList<GraphicsSlider> All { get; } =
    [
        new("GraphicsSliderShadowQualityLabel", "ShadowQuality",
        [
            new("GraphicsTierLow", "Low", [
                new("graphics.shadowmode", "2"), new("graphics.shadowlights", "0"),
                new("graphics.contactshadows", "False"), new("graphicssettings.shadowqualitypreset", "0")
            ]),
            new("GraphicsTierMedium", "Medium", [
                new("graphics.shadowmode", "3"), new("graphics.shadowlights", "1"),
                new("graphics.contactshadows", "False"), new("graphicssettings.shadowqualitypreset", "1")
            ]),
            new("GraphicsTierHigh", "High", [
                new("graphics.shadowmode", "4"), new("graphics.shadowlights", "2"),
                new("graphics.contactshadows", "True"), new("graphicssettings.shadowqualitypreset", "3")
            ])
        ]),

        new("GraphicsSliderTextureQualityLabel", "TextureQuality",
        [
            new("GraphicsTierLow", "Low", [
                new("graphicssettings.globaltexturemipmaplimit", "2"), new("graphics.af", "4"),
                new("graphics.lodbias", "0.5"), new("graphics.shaderlod", "1")
            ]),
            new("GraphicsTierMedium", "Medium", [
                new("graphicssettings.globaltexturemipmaplimit", "1"), new("graphics.af", "8"),
                new("graphics.lodbias", "1"), new("graphics.shaderlod", "3")
            ]),
            new("GraphicsTierHigh", "High", [
                new("graphicssettings.globaltexturemipmaplimit", "0"), new("graphics.af", "16"),
                new("graphics.lodbias", "2.5"), new("graphics.shaderlod", "5")
            ])
        ]),

        new("GraphicsSliderEffectsQualityLabel", "EffectsQuality",
        [
            new("GraphicsTierLow", "Low", [
                new("effects.antialiasing", "0"), new("effects.ao", "False"), new("effects.bloom", "False"),
                new("effects.shafts", "False"), new("graphics.volumetric_clouds", "0"), new("graphicssettings.pixellightcount", "0")
            ]),
            new("GraphicsTierMedium", "Medium", [
                new("effects.antialiasing", "3"), new("effects.ao", "True"), new("effects.bloom", "False"),
                new("effects.shafts", "False"), new("graphics.volumetric_clouds", "0"), new("graphicssettings.pixellightcount", "0")
            ]),
            new("GraphicsTierHigh", "High", [
                new("effects.antialiasing", "3"), new("effects.ao", "True"), new("effects.bloom", "True"),
                new("effects.shafts", "True"), new("graphics.volumetric_clouds", "1"), new("graphicssettings.pixellightcount", "4")
            ])
        ]),

        new("GraphicsSliderDrawDistanceLabel", "DrawDistance",
        [
            new("GraphicsTierLow", "Low", [
                new("graphics.drawdistance", "500"), new("render.instancing_render_distance", "500"),
                new("graphicssettings.particleraycastbudget", "4"), new("particle.quality", "0")
            ]),
            new("GraphicsTierMedium", "Medium", [
                new("graphics.drawdistance", "1000"), new("render.instancing_render_distance", "1500"),
                new("graphicssettings.particleraycastbudget", "4"), new("particle.quality", "100")
            ]),
            new("GraphicsTierHigh", "High", [
                new("graphics.drawdistance", "1500"), new("render.instancing_render_distance", "2000"),
                new("graphicssettings.particleraycastbudget", "512"), new("particle.quality", "100")
            ])
        ]),

        new("GraphicsSliderWorldDetailLabel", "WorldDetail",
        [
            new("GraphicsTierLow", "Low", [
                new("mesh.quality", "40"), new("grass.quality", "0"), new("tree.meshes", "40"),
                new("tree.quality", "75"), new("terrain.quality", "50")
            ]),
            new("GraphicsTierMedium", "Medium", [
                new("mesh.quality", "110"), new("grass.quality", "100"), new("tree.meshes", "100"),
                new("tree.quality", "125"), new("terrain.quality", "75")
            ]),
            new("GraphicsTierHigh", "High", [
                new("mesh.quality", "125"), new("grass.quality", "100"), new("tree.meshes", "100"),
                new("tree.quality", "150"), new("terrain.quality", "75")
            ])
        ]),

        new("GraphicsSliderWaterQualityLabel", "WaterQuality",
        [
            new("GraphicsTierLow", "Low", [new("water.quality", "0"), new("water.reflections", "0")]),
            new("GraphicsTierMedium", "Medium", [new("water.quality", "1"), new("water.reflections", "0")]),
            new("GraphicsTierHigh", "High", [new("water.quality", "2"), new("water.reflections", "1")])
        ]),

        new("GraphicsSliderInventoryDisplayLabel", "InventoryDisplay",
        [
            new("GraphicsTierLow", "Low", [new("ui.showinventoryplayer", "False"), new("ui.inventoryplayerquality", "0")]),
            new("GraphicsTierMedium", "Medium", [new("ui.showinventoryplayer", "True"), new("ui.inventoryplayerquality", "0")]),
            new("GraphicsTierHigh", "High", [new("ui.showinventoryplayer", "True"), new("ui.inventoryplayerquality", "2")])
        ])
    ];
}