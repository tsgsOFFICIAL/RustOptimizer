using System.Collections.Generic;

namespace RustOptimizer.Interface;

/// <summary>
/// One key or key-combo token as keys.cfg writes it: a bare key (<c>"e"</c>, <c>Modifier</c> null)
/// or a modifier+key combo (<c>"[leftctrl+1]"</c>, <c>Modifier</c> "leftctrl", <c>Key</c> "1").
/// Both halves are lowercase, matching keys.cfg's own convention.
/// </summary>
public readonly record struct KeyToken(string? Modifier, string Key)
{
    /// <summary>Formats back to keys.cfg's own token syntax - <c>"e"</c> or <c>"[leftctrl+1]"</c>.</summary>
    public string ToBindToken() => Modifier is null ? Key : $"[{Modifier}+{Key}]";
}

/// <summary>
/// One line of keys.cfg: a key/combo token bound to a command. <see cref="Command"/> is always the
/// exact, unparsed remainder of the line - a single console command, a semicolon-chained list, or a
/// <c>~meta.exec "A" "B"</c> toggle macro are all just opaque text here, never split or interpreted.
/// <see cref="ActionLabelKey"/>/<see cref="CategoryKey"/> are <see langword="null"/> when
/// <see cref="Command"/> doesn't exactly match a <see cref="RustAction"/> in the catalog - the row is
/// still shown, under an "Other" category, with the raw command as its own label rather than being hidden.
/// </summary>
public readonly record struct KeyBindingRow(KeyToken Token, string Command, string? ActionLabelKey, string? CategoryKey);

/// <summary>
/// One known, real Rust action from the catalog: the exact command string keys.cfg would use, paired
/// with a friendly name, one-line description, and category for display/picking. <see cref="Command"/>
/// is matched against a <see cref="KeyBindingRow.Command"/> by exact string equality only - never fuzzy
/// or prefix matching, since e.g. <c>"+use"</c> and <c>"use"</c> are different commands.
/// </summary>
public readonly record struct RustAction(string Command, string LabelKey, string CategoryKey, string DescriptionKey);

/// <summary>
/// One line within a <see cref="BindStage"/> of a manually-built macro. <see cref="RawText"/> is
/// always the exact literal text placed in that stage - either standalone (single-line stage) or as
/// one quoted <c>meta.exec</c> argument (multi-line stage) - never re-derived from
/// <see cref="ConvarKey"/> at build time, matching every other write path's opaque-string discipline.
/// <see cref="ConvarKey"/> is set only when this line came from <see cref="ConvarEditorCatalog"/>, so
/// the builder can re-show its slider/toggle/text editor if the user reopens it - purely a UI
/// convenience, never consulted when assembling the final command.
/// </summary>
public readonly record struct BindCommandLine(string? ConvarKey, string RawText);

/// <summary>One stage of a cycling bind - one or more <see cref="BindCommandLine"/>s that fire together on that stage's turn.</summary>
public readonly record struct BindStage(IReadOnlyList<BindCommandLine> Lines);

/// <summary>
/// Reads and writes Rust's keys.cfg - individual key bindings, as opposed to
/// <see cref="IConfigService"/>'s client.cfg convars. Every write is guarded the same way
/// <see cref="IConfigService.SetConvars"/> guards client.cfg: refused while Rust is running, backed
/// up first via <see cref="IConfigBackupService"/> (<see cref="ConfigBackupType.Keybinds"/>) unless
/// told not to.
/// </summary>
public interface IKeybindsService
{
    /// <summary>Every bind line currently in keys.cfg, in file order. Empty if Rust isn't installed or the file is missing.</summary>
    IReadOnlyList<KeyBindingRow> GetKeyBindings();

    /// <summary>Every known Rust action the picker can offer, in catalog order.</summary>
    IReadOnlyList<RustAction> GetKnownActions();

    /// <summary>
    /// Binds <paramref name="token"/> to <paramref name="command"/>, replacing that token's existing
    /// line if it already has one or adding a new line otherwise. Returns <see langword="false"/>
    /// (without writing anything) if Rust is currently running, not installed, or keys.cfg is missing.
    /// </summary>
    bool SetKeyBinding(KeyToken token, string command, bool createBackup = true);

    /// <summary>
    /// Removes whatever line currently binds <paramref name="token"/>, if any. Same guards as
    /// <see cref="SetKeyBinding"/>. Returns <see langword="true"/> if the token had no binding to
    /// begin with, since the end state - "nothing bound" - was already reached.
    /// </summary>
    bool RemoveKeyBinding(KeyToken token, bool createBackup = true);
}