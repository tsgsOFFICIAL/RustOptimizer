using System.Collections.Generic;
using RustOptimizer.Interface;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Convar values for each <see cref="ConfigPreset"/>.
/// </summary>
internal static class RustConfigPresets
{
    /// <summary>Convars for <paramref name="preset"/>, layered on top of <see cref="Common"/>.</summary>
    public static IReadOnlyDictionary<string, string> GetConvars(ConfigPreset preset)
    {
        Dictionary<string, string> presetConvars = preset switch
        {
            ConfigPreset.LowEndPc => LowEndPc,
            ConfigPreset.Competitive => Competitive,
            ConfigPreset.Cinematic => Cinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };

        // Common forms the base layer; preset-specific values are applied on top and win on conflict.
        Dictionary<string, string> merged = new(Common, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> kv in presetConvars)
            merged[kv.Key] = kv.Value;

        return merged;
    }

    /// <summary>Applied before every preset, regardless of which one is chosen.</summary>
    private static readonly Dictionary<string, string> Common = new()
    {
        ["system.auto_cpu_affinity"] = "True",
        ["graphics.dof"] = "False",
        ["culling.toggle"] = "True",
        ["gc.buffer"] = "4096",
        ["fps.limit"] = "0",
        ["graphics.screenmode"] = "1",
        ["rgbeffects.enabled"] = "False",
        ["player.recoilcomp"] = "False",
        ["decor.quality"] = "0",
        ["graphicssettings.softparticles"] = "True"
    };

    private static readonly Dictionary<string, string> LowEndPc = new()
    {
        ["graphicssettings.globaltexturemipmaplimit"] = "2", // 1/4 Resolution
        ["graphics.contactshadows"] = "False",
        ["effects.antialiasing"] = "0",
        ["effects.ao"] = "False",
        ["effects.bloom"] = "False",
        ["effects.shafts"] = "False",
        ["graphics.af"] = "4",
        ["graphics.drawdistance"] = "500",
        ["graphics.lodbias"] = "0.5",
        ["graphics.shaderlod"] = "1",
        ["graphics.shadowlights"] = "1",
        ["graphics.shadowmode"] = "3",
        ["graphics.volumetric_clouds"] = "0",
        ["graphicssettings.particleraycastbudget"] = "4",
        ["graphicssettings.pixellightcount"] = "0",
        ["graphicssettings.shadowqualitypreset"] = "1",
        ["mesh.quality"] = "40",
        ["grass.quality"] = "0",
        ["particle.quality"] = "0",
        ["render.instancing_render_distance"] = "500",
        ["tree.meshes"] = "40",
        ["tree.quality"] = "75",
        ["terrain.quality"] = "50",
        ["ui.showinventoryplayer"] = "False",
        ["ui.inventoryplayerquality"] = "0",
        ["water.quality"] = "0",
        ["water.reflections"] = "0"
    };

    private static readonly Dictionary<string, string> Competitive = new()
    {
        ["graphicssettings.globaltexturemipmaplimit"] = "1", // 1/2 Resolution
        ["graphics.contactshadows"] = "False",
        ["effects.antialiasing"] = "3",
        ["effects.ao"] = "True",
        ["effects.bloom"] = "False",
        ["effects.shafts"] = "False",
        ["graphics.af"] = "8",
        ["graphics.drawdistance"] = "1000",
        ["graphics.lodbias"] = "1",
        ["graphics.shaderlod"] = "3",
        ["graphics.shadowlights"] = "1",
        ["graphics.shadowmode"] = "3",
        ["graphics.volumetric_clouds"] = "0",
        ["graphicssettings.particleraycastbudget"] = "4",
        ["graphicssettings.pixellightcount"] = "0",
        ["graphicssettings.shadowqualitypreset"] = "1",
        ["mesh.quality"] = "110",
        ["grass.quality"] = "100",
        ["particle.quality"] = "100",
        ["render.instancing_render_distance"] = "1500",
        ["tree.meshes"] = "100",
        ["tree.quality"] = "125",
        ["terrain.quality"] = "75",
        ["ui.showinventoryplayer"] = "True",
        ["ui.inventoryplayerquality"] = "0",
        ["water.quality"] = "0",
        ["water.reflections"] = "0"
    };

    private static readonly Dictionary<string, string> Cinematic = new()
    {
        ["graphicssettings.globaltexturemipmaplimit"] = "0", // 1/1 Resolution
        ["graphics.contactshadows"] = "True",
        ["effects.antialiasing"] = "3",
        ["effects.ao"] = "True",
        ["effects.bloom"] = "True",
        ["effects.shafts"] = "True",
        ["graphics.af"] = "16",
        ["graphics.drawdistance"] = "1500",
        ["graphics.lodbias"] = "2.5",
        ["graphics.shaderlod"] = "5",
        ["graphics.shadowlights"] = "2",
        ["graphics.shadowmode"] = "4",
        ["graphics.volumetric_clouds"] = "1",
        ["graphicssettings.particleraycastbudget"] = "512",
        ["graphicssettings.pixellightcount"] = "4",
        ["graphicssettings.shadowqualitypreset"] = "3",
        ["mesh.quality"] = "125",
        ["grass.quality"] = "100",
        ["particle.quality"] = "100",
        ["render.instancing_render_distance"] = "2000",
        ["tree.meshes"] = "100",
        ["tree.quality"] = "150",
        ["terrain.quality"] = "75",
        ["ui.showinventoryplayer"] = "True",
        ["ui.inventoryplayerquality"] = "2",
        ["water.quality"] = "2",
        ["water.reflections"] = "1"
    };
}