using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Text.Json;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IConfigBackupService" />
[SupportedOSPlatform("windows")]
public sealed class ConfigBackupService(IRustProcessService rustProcess) : IConfigBackupService
{
    private const string ManifestFileName = "manifest.json";

    private static readonly string BackupsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "Backups");

    public IReadOnlyList<ConfigBackupInfo> GetBackups(ConfigBackupType type)
    {
        string folder = GetTypeFolder(type);
        List<ManifestEntry> manifest = LoadManifest(folder);
        List<ManifestEntry> stillValid = [];
        List<ConfigBackupInfo> result = [];

        foreach (ManifestEntry entry in manifest)
        {
            string path = Path.Combine(folder, entry.FileName);
            if (!File.Exists(path))
                continue;

            stillValid.Add(entry);
            result.Add(new ConfigBackupInfo(entry.FileName, entry.Label, entry.Label is null, entry.CreatedUtc, new FileInfo(path).Length));
        }

        // Self-heal: a backup file removed from outside the app (rather than via Delete()) would
        // otherwise leave a dead entry in the manifest forever, since nothing else ever prunes it.
        if (stillValid.Count != manifest.Count)
            SaveManifest(folder, stillValid);

        return result.OrderByDescending(backup => backup.CreatedUtc).ToList();
    }

    public bool CreateBackup(ConfigBackupType type, string? label)
    {
        string? installPath = rustProcess.GetInstallPath();
        if (installPath is null)
            return false;

        string sourcePath = Path.Combine(installPath, "cfg", GetSourceFileName(type));
        if (!File.Exists(sourcePath))
        {
            AppLog.Warn("ConfigBackupService", $"Source file not found at '{sourcePath}'.");
            return false;
        }

        try
        {
            string folder = GetTypeFolder(type);
            Directory.CreateDirectory(folder);

            DateTime nowUtc = DateTime.UtcNow;
            string shortId = Guid.NewGuid().ToString("N")[..8];
            string fileName = $"{nowUtc:yyyyMMdd-HHmmssfff}-{shortId}.cfg";
            File.Copy(sourcePath, Path.Combine(folder, fileName));

            List<ManifestEntry> manifest = LoadManifest(folder);
            manifest.Add(new ManifestEntry(fileName, string.IsNullOrWhiteSpace(label) ? null : label.Trim(), nowUtc));
            SaveManifest(folder, manifest);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigBackupService", $"Failed to create a '{type}' backup.", ex);
            return false;
        }
    }

    public bool Restore(ConfigBackupType type, string fileName)
    {
        if (rustProcess.IsRunning())
        {
            AppLog.Warn("ConfigBackupService", "Refused to restore a backup while Rust is running.");
            return false;
        }

        string? installPath = rustProcess.GetInstallPath();
        if (installPath is null)
            return false;

        string backupPath = Path.Combine(GetTypeFolder(type), fileName);
        if (!File.Exists(backupPath))
            return false;

        try
        {
            // Best-effort safety snapshot of what's about to be overwritten - failure here (e.g. the
            // live file doesn't exist yet) shouldn't block the restore itself.
            CreateBackup(type, label: null);

            string destinationPath = Path.Combine(installPath, "cfg", GetSourceFileName(type));
            File.Copy(backupPath, destinationPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigBackupService", $"Failed to restore '{type}' backup '{fileName}'.", ex);
            return false;
        }
    }

    public bool Delete(ConfigBackupType type, string fileName)
    {
        string folder = GetTypeFolder(type);

        try
        {
            string path = Path.Combine(folder, fileName);
            if (File.Exists(path))
                File.Delete(path);

            List<ManifestEntry> manifest = LoadManifest(folder);
            manifest.RemoveAll(entry => entry.FileName == fileName);
            SaveManifest(folder, manifest);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigBackupService", $"Failed to delete '{type}' backup '{fileName}'.", ex);
            return false;
        }
    }

    /// <summary>Maps a backup type to the cfg file it snapshots, both living directly under Rust's install "cfg" folder.</summary>
    private static string GetSourceFileName(ConfigBackupType type) => type switch
    {
        ConfigBackupType.Settings => "client.cfg",
        ConfigBackupType.Keybinds => "keys.cfg",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static string GetTypeFolder(ConfigBackupType type) => Path.Combine(BackupsRoot, type.ToString());

    private static List<ManifestEntry> LoadManifest(string folder)
    {
        string path = Path.Combine(folder, ManifestFileName);
        if (!File.Exists(path))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ManifestEntry>>(File.ReadAllText(path)) ?? [];
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigBackupService", $"Failed to read backup manifest '{path}'.", ex);
            return [];
        }
    }

    private static void SaveManifest(string folder, List<ManifestEntry> manifest)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ManifestFileName), JsonSerializer.Serialize(manifest));
    }

    /// <summary>One manifest record, persisted as JSON alongside the backup files it describes.</summary>
    private sealed record ManifestEntry(string FileName, string? Label, DateTime CreatedUtc);
}