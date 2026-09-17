using System.Text.RegularExpressions;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IKeybindsService" />
[SupportedOSPlatform("windows")]
public sealed class KeybindsService(IRustProcessService rustProcess, IConfigBackupService configBackup) : IKeybindsService
{
    // Captures the key/combo token (group 1, never contains whitespace) and the entire remainder of
    // the line (group 2) as one opaque blob - never split further, never touched by anything but
    // plain string formatting when writing. See RustActionCatalog's doc comment for why.
    private static readonly Regex BindLineRegex = new(@"^bind\s+(\S+)\s+(.+)$", RegexOptions.Compiled);

    private static readonly Dictionary<string, RustAction> ActionsByCommand =
        RustActionCatalog.All.ToDictionary(action => action.Command, action => action, StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<KeyBindingRow> GetKeyBindings()
    {
        string? path = GetKeysConfigPath();
        if (path is null || !File.Exists(path))
            return [];

        List<KeyBindingRow> rows = [];
        foreach (string line in File.ReadLines(path))
        {
            Match match = BindLineRegex.Match(line);
            if (!match.Success)
                continue;

            KeyToken token = ParseToken(match.Groups[1].Value);
            string command = match.Groups[2].Value;
            RustAction? action = ActionsByCommand.TryGetValue(command, out RustAction found) ? found : null;

            rows.Add(new KeyBindingRow(token, command, action?.LabelKey, action?.CategoryKey));
        }

        return rows;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Excludes every "~"-prefixed <c>meta.exec</c> cycling macro from <see cref="RustActionCatalog"/> -
    /// those are one player's own custom toggle binds, not generic Rust actions, so they don't belong
    /// in a "pick a known action" catalog. They still work for <em>naming</em> a bind that happens to
    /// match one (see <see cref="GetKeyBindings"/>'s use of <see cref="ActionsByCommand"/>) - they're
    /// only hidden from the browsable list here. See <see cref="BindMacroExamples"/> for where they
    /// resurface, as loadable starting points in the manual macro builder instead.
    /// </remarks>
    public IReadOnlyList<RustAction> GetKnownActions() =>
        RustActionCatalog.All.Where(action => !action.Command.StartsWith('~')).ToList();

    /// <inheritdoc />
    public bool SetKeyBinding(KeyToken token, string command, bool createBackup = true)
    {
        if (rustProcess.IsRunning())
        {
            AppLog.Warn("KeybindsService", "Refused to write keys.cfg while Rust is running.");
            return false;
        }

        string? path = GetKeysConfigPath();
        if (path is null || !File.Exists(path))
        {
            AppLog.Warn("KeybindsService", $"keys.cfg not found at '{path}'.");
            return false;
        }

        try
        {
            string targetToken = token.ToBindToken();
            List<string> lines = [.. File.ReadAllLines(path)];
            int existingIndex = lines.FindIndex(line => IsBindFor(line, targetToken));

            if (createBackup && !configBackup.CreateBackup(ConfigBackupType.Keybinds, label: null))
                return false;

            string newLine = $"bind {targetToken} {command}";
            if (existingIndex >= 0)
                lines[existingIndex] = newLine;
            else
                lines.Add(newLine);

            File.WriteAllLines(path, lines);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("KeybindsService", "Failed to write a key binding to keys.cfg.", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public bool RemoveKeyBinding(KeyToken token, bool createBackup = true)
    {
        if (rustProcess.IsRunning())
        {
            AppLog.Warn("KeybindsService", "Refused to write keys.cfg while Rust is running.");
            return false;
        }

        string? path = GetKeysConfigPath();
        if (path is null || !File.Exists(path))
            return false;

        try
        {
            string targetToken = token.ToBindToken();
            List<string> lines = [.. File.ReadAllLines(path)];
            int index = lines.FindIndex(line => IsBindFor(line, targetToken));

            // Already unbound - the end state this call asks for already holds, so there's nothing
            // to do and nothing to back up.
            if (index < 0)
                return true;

            if (createBackup && !configBackup.CreateBackup(ConfigBackupType.Keybinds, label: null))
                return false;

            lines.RemoveAt(index);
            File.WriteAllLines(path, lines);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("KeybindsService", "Failed to remove a key binding from keys.cfg.", ex);
            return false;
        }
    }

    /// <summary>Whether <paramref name="line"/> is a bind line for exactly <paramref name="targetToken"/> (as formatted by <see cref="KeyToken.ToBindToken"/>).</summary>
    private static bool IsBindFor(string line, string targetToken)
    {
        Match match = BindLineRegex.Match(line);
        return match.Success && match.Groups[1].Value == targetToken;
    }

    /// <summary>Parses a raw key token from keys.cfg - either a bare key or a bracketed <c>[modifier+key]</c> combo.</summary>
    private static KeyToken ParseToken(string raw)
    {
        if (raw.Length > 2 && raw[0] == '[' && raw[^1] == ']')
        {
            string inner = raw[1..^1];
            int plusIndex = inner.IndexOf('+');
            if (plusIndex > 0)
                return new KeyToken(inner[..plusIndex], inner[(plusIndex + 1)..]);
        }

        return new KeyToken(null, raw);
    }

    /// <summary>Resolves keys.cfg's full path, or <see langword="null"/> if Rust's install path can't be found.</summary>
    private string? GetKeysConfigPath()
    {
        string? installPath = rustProcess.GetInstallPath();
        return installPath is null ? null : Path.Combine(installPath, "cfg", "keys.cfg");
    }
}