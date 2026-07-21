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
        ["graphics.contactshadows"] = "False",
        ["graphics.dof"] = "False",
        ["culling.toggle"] = "True",
        ["gc.buffer"] = "4096",
        ["fps.limit"] = "0",
        ["graphics.screenmode"] = "0", // TODO: Verify 0 or 1 (I have 1 on my gaming rig but 0 is default)
        ["rgbeffects.enabled"] = "False",
        ["player.recoilcomp"] = "False"
    };

    private static readonly Dictionary<string, string> LowEndPc = new()
    {
        ["decor.quality"] = "0",
        ["effects.ao"] = "False",
        ["effects.bloom"] = "False",
        ["effects.shafts"] = "False",
        ["graphics.af"] = "1",
        ["graphics.drawdistance"] = "500",
        ["graphics.lodbias"] = "0.5",
        ["graphics.shaderlod"] = "1",
        ["graphics.shadowlights"] = "0",
        ["graphics.shadowmode"] = "1",
        ["graphics.volumetric_clouds"] = "0",
        ["graphicssettings.particleraycastbudget"] = "0",
        ["graphicssettings.pixellightcount"] = "0",
        ["graphicssettings.shadowqualitypreset"] = "0",
        ["graphicssettings.softparticles"] = "False",
        ["grass.quality"] = "0",
        ["mesh.quality"] = "40",
        ["particle.quality"] = "0",
        ["render.instancing_render_distance"] = "500",
        ["terrain.quality"] = "20",
        ["tree.meshes"] = "20",
        ["tree.quality"] = "20",
        ["water.quality"] = "0",
        ["water.reflections"] = "0"
    };

    private static readonly Dictionary<string, string> Competitive = new()
    {
        ["decor.quality"] = "0",
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
        ["graphicssettings.softparticles"] = "True",
        ["grass.quality"] = "100",
        ["mesh.quality"] = "110",
        ["particle.quality"] = "100",
        ["render.instancing_render_distance"] = "1500",
        ["terrain.quality"] = "75",
        ["tree.meshes"] = "100",
        ["tree.quality"] = "125",
        ["water.quality"] = "0",
        ["water.reflections"] = "0"
    };

    private static readonly Dictionary<string, string> Cinematic = new()
    {
        ["decor.quality"] = "100",
        ["effects.ao"] = "True",
        ["effects.bloom"] = "True",
        ["effects.shafts"] = "True",
        ["graphics.af"] = "16",
        ["graphics.drawdistance"] = "2000",
        ["graphics.lodbias"] = "3",
        ["graphics.shaderlod"] = "6",
        ["graphics.shadowlights"] = "2",
        ["graphics.shadowmode"] = "3",
        ["graphics.volumetric_clouds"] = "3",
        ["graphicssettings.particleraycastbudget"] = "256",
        ["graphicssettings.pixellightcount"] = "3",
        ["graphicssettings.shadowqualitypreset"] = "3",
        ["graphicssettings.softparticles"] = "True",
        ["grass.quality"] = "100",
        ["mesh.quality"] = "200",
        ["particle.quality"] = "100",
        ["render.instancing_render_distance"] = "2000",
        ["terrain.quality"] = "100",
        ["tree.meshes"] = "150",
        ["tree.quality"] = "150",
        ["water.quality"] = "2",
        ["water.reflections"] = "1"
    };
}