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
public sealed class ConfigService(IRustProcessService rustProcess) : IConfigService
{
    public bool ApplyPreset(ConfigPreset preset)
    {
        if (rustProcess.IsRunning())
        {
            AppLog.Warn("ConfigService", "Refused to apply preset while Rust is running.");
            return false;
        }

        string? installPath = rustProcess.GetInstallPath();
        if (installPath == null)
            return false;

        string configPath = Path.Combine(installPath, "cfg", "client.cfg");
        if (!File.Exists(configPath))
        {
            AppLog.Warn("ConfigService", $"client.cfg not found at '{configPath}'.");
            return false;
        }

        try
        {
            Dictionary<string, string> remaining = new(RustConfigPresets.GetConvars(preset), StringComparer.OrdinalIgnoreCase);
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

            File.Copy(configPath, configPath + ".bak", overwrite: true);

            IEnumerable<string> finalLines = remaining.Count == 0
                ? lines
                : lines.Concat(remaining.Select(kv => $"{kv.Key} \"{kv.Value}\""));

            File.WriteAllLines(configPath, finalLines);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ConfigService", $"Failed to apply preset '{preset}'.", ex);
            return false;
        }
    }
}