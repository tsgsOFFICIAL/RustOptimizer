using System.Collections.Generic;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Convar values for each <see cref="ConfigPreset"/>. Values are sensible defaults derived from
/// the real convars found in an actual client.cfg (graphics.shadowmode/af/shaderlod are tiered
/// 0-3ish; grass.quality/water.quality read as 0-100-style sliders on this install). Kept separate from <see cref="ConfigService"/>'s mechanics.
/// </summary>
internal static class RustConfigPresets
{
    public static IReadOnlyDictionary<string, string> GetConvars(ConfigPreset preset) => preset switch
    {
        ConfigPreset.LowEndPc => LowEndPc,
        ConfigPreset.Competitive => Competitive,
        ConfigPreset.Streamer => Streamer,
        ConfigPreset.Cinematic => Cinematic,
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    private static readonly Dictionary<string, string> LowEndPc = new()
    {
        ["graphics.shadowmode"] = "0", ["graphics.shadowlights"] = "0", ["graphics.contactshadows"] = "False",
        ["graphics.dof"] = "False", ["graphics.drawdistance"] = "1000", ["graphics.af"] = "0",
        ["graphics.shaderlod"] = "0", ["graphics.renderscale"] = "0.75", ["graphics.hlod"] = "False",
        ["graphics.volumetric_clouds"] = "0", ["water.quality"] = "0", ["water.reflections"] = "0",
        ["grass.quality"] = "0", ["grass.distance"] = "50", ["grass.displacement"] = "False",
    };

    private static readonly Dictionary<string, string> Competitive = new()
    {
        ["graphics.shadowmode"] = "1", ["graphics.contactshadows"] = "False", ["graphics.dof"] = "False",
        ["graphics.drawdistance"] = "2000", ["graphics.af"] = "4", ["graphics.shaderlod"] = "3",
        ["graphics.renderscale"] = "1", ["graphics.volumetric_clouds"] = "0", ["water.quality"] = "0",
        ["water.reflections"] = "0", ["grass.quality"] = "0", ["grass.distance"] = "80",
        ["grass.displacement"] = "False", ["client.camfov"] = "90", ["client.crosshair"] = "True",
    };

    private static readonly Dictionary<string, string> Streamer = new()
    {
        ["graphics.shadowmode"] = "2", ["graphics.contactshadows"] = "False", ["graphics.dof"] = "False",
        ["graphics.drawdistance"] = "2500", ["graphics.af"] = "8", ["graphics.shaderlod"] = "4",
        ["graphics.renderscale"] = "1", ["graphics.volumetric_clouds"] = "0", ["water.quality"] = "1",
        ["water.reflections"] = "1", ["grass.quality"] = "50", ["grass.distance"] = "100",
        ["grass.displacement"] = "True", ["client.camfov"] = "80",
    };

    private static readonly Dictionary<string, string> Cinematic = new()
    {
        ["graphics.shadowmode"] = "3", ["graphics.contactshadows"] = "True", ["graphics.dof"] = "True",
        ["graphics.drawdistance"] = "3500", ["graphics.af"] = "16", ["graphics.shaderlod"] = "6",
        ["graphics.renderscale"] = "1", ["graphics.volumetric_clouds"] = "1", ["water.quality"] = "2",
        ["water.reflections"] = "1", ["grass.quality"] = "100", ["grass.distance"] = "150",
        ["grass.displacement"] = "True", ["client.camfov"] = "70",
    };
}