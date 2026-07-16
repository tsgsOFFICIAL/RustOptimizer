using System.Collections.Generic;
using System;

namespace RustOptimizer.Interface;

/// <summary>Which of Rust's cfg files a backup applies to.</summary>
public enum ConfigBackupType
{
    /// <summary>client.cfg - graphics/client convars, including the Dashboard's preset profiles.</summary>
    Settings,

    /// <summary>keys.cfg - key bindings.</summary>
    Keybinds
}

/// <summary>
/// One stored backup: either a user-named manual snapshot, or an automatic one taken before a
/// preset apply or a restore overwrites the live file (<see cref="Label"/> is <see langword="null"/>
/// for the latter).
/// </summary>
public readonly record struct ConfigBackupInfo(string FileName, string? Label, bool IsAutomatic, DateTime CreatedUtc, long SizeBytes);

/// <summary>
/// Stores and restores timestamped backups of Rust's client.cfg and keys.cfg, independent of the
/// Rust install itself - the backup history lives under the same app-data folder as the theme/language
/// preference files, so it survives a Rust reinstall or verify.
/// </summary>
public interface IConfigBackupService
{
    /// <summary>Every backup on hand for the given type, newest first.</summary>
    IReadOnlyList<ConfigBackupInfo> GetBackups(ConfigBackupType type);

    /// <summary>
    /// Snapshots the live file for <paramref name="type"/> into the backup history.
    /// <paramref name="label"/> null/empty marks the backup automatic; a non-empty value marks it
    /// a named manual backup. Returns <see langword="false"/> if Rust isn't installed or the file
    /// doesn't exist.
    /// </summary>
    bool CreateBackup(ConfigBackupType type, string? label);

    /// <summary>
    /// Restores a stored backup over the live file, after first snapshotting the file being
    /// overwritten (automatic, so a restore is itself reversible). Returns <see langword="false"/>
    /// without writing anything if Rust is currently running, not installed, or the backup is missing.
    /// </summary>
    bool Restore(ConfigBackupType type, string fileName);

    /// <summary>Deletes a stored backup. Returns <see langword="false"/> if it couldn't be removed.</summary>
    bool Delete(ConfigBackupType type, string fileName);
}