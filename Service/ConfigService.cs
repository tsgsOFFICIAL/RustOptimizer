using System.Text.RegularExpressions;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IConfigService" />
[SupportedOSPlatform("windows")]
public sealed class ConfigService(IRustProcessService rustProcess, IConfigBackupService configBackup) : IConfigService
{
    /// <inheritdoc />
    public bool ApplyPreset(ConfigPreset preset) => SetConvars(RustConfigPresets.GetConvars(preset));

    /// <inheritdoc />
    public IReadOnlyList<GameplayTweak> GetRecommendedGameplayTweaks() => RecommendedGameplayTweaks.All;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ReadConvars(IReadOnlyCollection<string> convars)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        string? configPath = GetClientConfigPath();
        if (configPath is null || !File.Exists(configPath))
            return result;

        HashSet<string> wanted = new(convars, StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadLines(configPath))
        {
            Match match = Regex.Match(line, "^(\\S+)\\s+\"([^\"]*)\"");
            if (match.Success && wanted.Contains(match.Groups[1].Value))
                result[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return result;
    }

    /// <inheritdoc />
    public bool SetConvars(IReadOnlyDictionary<string, string> convars, bool createBackup = true)
    {
        if (rustProcess.IsRunning())
        {
            AppLog.Warn("ConfigService", "Refused to write client.cfg while Rust is running.");
            return false;
        }

        string? configPath = GetClientConfigPath();
        if (configPath is null)
            return false;

        if (!File.Exists(configPath))
        {
            AppLog.Warn("ConfigService", $"client.cfg not found at '{configPath}'.");
            return false;
        }

        try
        {
            Dictionary<string, string> remaining = new(convars, StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(configPath);

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = Regex.Match(lines[i], "^(\\S+)\\s+\"");
                if (match.Success && remaining.TryGetValue(match.Groups[1].Value, out string? value))
                {
                    lines[i] = $"{match.Groups[1].Value} \"{value}\"";
                    remaining.Remove(match.Groups[1].Value);
                }
            }

            if (createBackup && !configBackup.CreateBackup(ConfigBackupType.Settings, label: null))
                return false;

            IEnumerable<string> finalLines = remaining.Count == 0
                ? lines
                : lines.Concat(remaining.Select(kv => $"{kv.Key} \"{kv.Value}\""));

            File.WriteAllLines(configPath, finalLines);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigService", "Failed to write convars to client.cfg.", ex);
            return false;
        }
    }

    /// <summary>Resolves client.cfg's full path, or <see langword="null"/> if Rust's install path can't be found.</summary>
    private string? GetClientConfigPath()
    {
        string? installPath = rustProcess.GetInstallPath();
        return installPath is null ? null : Path.Combine(installPath, "cfg", "client.cfg");
    }
}