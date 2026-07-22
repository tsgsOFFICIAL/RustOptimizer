using System.Collections.Generic;

namespace RustOptimizer.Interface;

public enum ConfigPreset
{
    LowEndPc,
    Competitive,
    Cinematic
}

/// <summary>One convar's on/off values for a <see cref="GameplayTweak"/> - most tweaks are a single entry, but a few need more than one convar set together to have any effect.</summary>
public readonly record struct ConvarValue(string Convar, string EnabledValue, string DisabledValue);

/// <summary>
/// Which section of the Gameplay page a <see cref="GameplayTweak"/> belongs in:
/// <see cref="RecommendedForEveryone"/> has no real downside regardless of taste or hardware,
/// while <see cref="Preference"/> trades something (info density, personal feel) that players could disagree on.
/// </summary>
public enum GameplayTweakCategory
{
    RecommendedForEveryone,
    Preference
}

/// <summary>
/// One optional Rust gameplay/UX tweak with no meaningful performance cost; a visibility or
/// clarity improvement rather than a graphics/performance preset. <see cref="LabelKey"/> and
/// <see cref="DescriptionKey"/> are localization keys.
/// </summary>
public readonly record struct GameplayTweak(IReadOnlyList<ConvarValue> Convars, string LabelKey, string DescriptionKey, GameplayTweakCategory Category);

/// <summary>One convar's value for a specific <see cref="GraphicsSliderTier"/>.</summary>
public readonly record struct ConvarSetting(string Convar, string Value);

/// <summary>
/// One selectable quality level for a <see cref="GraphicsSlider"/> (e.g. "Low"/"Medium"/"High").
/// <see cref="LabelKey"/> is a localization key. <see cref="PreviewId"/> is a stable,
/// language-independent identifier (e.g. "Low") used together with the owning
/// <see cref="GraphicsSlider.PreviewId"/> to look up an in-game preview screenshot -
/// see <c>Assets/GraphicsPreviews/README.md</c> for the naming convention.
/// </summary>
public readonly record struct GraphicsSliderTier(string LabelKey, string PreviewId, IReadOnlyList<ConvarSetting> Values);

/// <summary>
/// One simplified graphics quality control on the Graphics page (e.g. "Shadow Quality"), grouping
/// several related convars behind a handful of named tiers instead of exposing each convar
/// individually - a friendlier alternative to <see cref="ConfigPreset"/>'s whole-bundle presets.
/// <see cref="TitleKey"/> is a localization key. <see cref="PreviewId"/> is a stable,
/// language-independent identifier (e.g. "ShadowQuality") used for preview screenshot lookup.
/// </summary>
public readonly record struct GraphicsSlider(string TitleKey, string PreviewId, IReadOnlyList<GraphicsSliderTier> Tiers);

/// <summary>
/// Applies preset graphics/performance settings, individually toggleable gameplay tweaks, and
/// simplified graphics quality sliders, to Rust's client.cfg.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Applies a preset's convar values to Rust's client.cfg, leaving every other setting
    /// untouched. Backs up the existing file to client.cfg.bak first. Returns <see langword="false"/>
    /// (without writing anything) if Rust is currently running, not installed, or client.cfg is missing.
    /// </summary>
    bool ApplyPreset(ConfigPreset preset);

    /// <summary>Every recommended gameplay tweak this app knows about, in display order.</summary>
    IReadOnlyList<GameplayTweak> GetRecommendedGameplayTweaks();

    /// <summary>Every simplified graphics quality slider shown on the Graphics page, in display order.</summary>
    IReadOnlyList<GraphicsSlider> GetGraphicsSliders();

    /// <summary>
    /// Reads the current values of the given convars from client.cfg. Convars not found in the
    /// file (or if client.cfg/Rust's install can't be resolved) are simply absent from the result.
    /// </summary>
    IReadOnlyDictionary<string, string> ReadConvars(IReadOnlyCollection<string> convars);

    /// <summary>
    /// Writes the given convar values to Rust's client.cfg, leaving every other setting untouched -
    /// the same mechanism <see cref="ApplyPreset"/> uses, just for an arbitrary set of convars (e.g.
    /// one gameplay tweak's convar(s)) rather than a whole preset. Same Rust-running/install/missing-file
    /// guards as <see cref="ApplyPreset"/>. <paramref name="createBackup"/> defaults to <see langword="true"/>
    /// </summary>
    bool SetConvars(IReadOnlyDictionary<string, string> convars, bool createBackup = true);
}