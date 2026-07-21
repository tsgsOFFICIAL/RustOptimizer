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

/// <summary>
/// Applies preset graphics/performance settings, and individually toggleable gameplay tweaks, to
/// Rust's client.cfg.
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