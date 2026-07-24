using System.Collections.Generic;
using RustOptimizer.Interface;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One entry in the Graphics page's profile dropdown - either a built-in preset or a user-saved
/// <see cref="GraphicsProfile"/>. Both carry a <see cref="TierBySlider"/> map (slider
/// <c>PreviewId</c> → tier <c>PreviewId</c>) so the current slider state can be compared against any
/// profile uniformly, regardless of where it came from.
/// </summary>
public sealed class GraphicsProfileOption
{
    private GraphicsProfileOption(string name, bool isBuiltIn, ConfigPreset preset, GraphicsProfile? custom, IReadOnlyDictionary<string, string> tierBySlider)
    {
        Name = name;
        IsBuiltIn = isBuiltIn;
        Preset = preset;
        Custom = custom;
        TierBySlider = tierBySlider;
    }

    /// <summary>Creates the option for a built-in preset, with its resolved tier-per-slider map.</summary>
    public static GraphicsProfileOption ForBuiltIn(string name, ConfigPreset preset, IReadOnlyDictionary<string, string> tierBySlider)
        => new(name, isBuiltIn: true, preset, custom: null, tierBySlider);

    /// <summary>Creates the option for a user-saved custom profile.</summary>
    public static GraphicsProfileOption ForCustom(GraphicsProfile custom)
        => new(custom.Name, isBuiltIn: false, default, custom, custom.Sliders);

    /// <summary>The profile's display name, shown in the dropdown.</summary>
    public string Name { get; }

    /// <summary>Whether this is one of the three built-in presets - built-ins can't be overridden, renamed, or deleted.</summary>
    public bool IsBuiltIn { get; }

    /// <summary>The preset this option applies, when <see cref="IsBuiltIn"/> is <see langword="true"/>.</summary>
    public ConfigPreset Preset { get; }

    /// <summary>The backing custom profile, or <see langword="null"/> for a built-in.</summary>
    public GraphicsProfile? Custom { get; }

    /// <summary>The tier this profile sets each slider to: slider <c>PreviewId</c> → tier <c>PreviewId</c>.</summary>
    public IReadOnlyDictionary<string, string> TierBySlider { get; }
}