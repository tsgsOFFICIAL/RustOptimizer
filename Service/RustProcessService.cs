using RustOptimizer.Service.Logging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Diagnostics;
using Microsoft.Win32;
using System.Linq;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IRustProcessService" />
[SupportedOSPlatform("windows")]
public sealed class RustProcessService : IRustProcessService
{
    private const string ProcessName = "RustClient";
    private const string SteamAppId = "252490";

    private string? _installPath;

    public bool IsRunning()
    {
        Process[] processes = Process.GetProcessesByName(ProcessName);
        foreach (Process process in processes)
            process.Dispose();

        return processes.Length > 0;
    }

    public void Launch() => Utility.OpenUrl($"steam://rungameid/{SteamAppId}");

    public void VerifyFiles() => Utility.OpenUrl($"steam://validate/{SteamAppId}");

    public string? GetInstallPath()
    {
        if (_installPath != null)
            return _installPath;

        try
        {
            string? steamPath = GetSteamPath();
            if (steamPath == null)
                return null;

            foreach (string library in GetLibraryFolders(steamPath))
            {
                string manifestPath = Path.Combine(library, "steamapps", $"appmanifest_{SteamAppId}.acf");
                if (!File.Exists(manifestPath))
                    continue;

                string manifest = File.ReadAllText(manifestPath);
                string? installDir = ExtractVdfValue(manifest, "installdir");
                if (installDir == null)
                    continue;

                string candidate = Path.GetFullPath(Path.Combine(library, "steamapps", "common", installDir));
                if (Directory.Exists(candidate))
                    return _installPath = candidate;
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("RustProcessService", "Failed to resolve Rust's install path.", ex);
        }

        return null;
    }

    /// <summary>
    /// Reads Steam's own install path from the registry. Only the registry is consulted -
    /// no hardcoded fallback paths are probed if both keys are missing.
    /// </summary>
    private static string? GetSteamPath()
    {
        try
        {
            using RegistryKey? hkcu = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (hkcu?.GetValue("SteamPath") is string userPath && !string.IsNullOrWhiteSpace(userPath))
                return userPath;
        }
        catch (Exception ex)
        {
            AppLog.Warn("RustProcessService", "Failed to read HKCU Steam registry key.", ex);
        }

        try
        {
            using RegistryKey? hklm = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (hklm?.GetValue("InstallPath") is string machinePath && !string.IsNullOrWhiteSpace(machinePath))
                return machinePath;
        }
        catch (Exception ex)
        {
            AppLog.Warn("RustProcessService", "Failed to read HKLM Steam registry key.", ex);
        }

        return null;
    }

    /// <summary>
    /// Enumerates every Steam library folder, starting with the default library (<paramref name="steamPath"/>
    /// itself) plus any additional ones listed in steamapps/libraryfolders.vdf.
    /// </summary>
    private static IEnumerable<string> GetLibraryFolders(string steamPath)
    {
        // Normalize here since Steam's registry value and VDF entries can use either slash style
        // (e.g. "c:/program files (x86)/steam");
        List<string> libraries = [Path.GetFullPath(steamPath)];

        string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
            return libraries;

        try
        {
            string content = File.ReadAllText(vdfPath);
            foreach (Match match in Regex.Matches(content, "\"path\"\\s*\"([^\"]+)\""))
            {
                // VDF escapes backslashes as \\ in Windows paths.
                string path = Path.GetFullPath(match.Groups[1].Value.Replace("\\\\", "\\"));
                if (!libraries.Contains(path, StringComparer.OrdinalIgnoreCase))
                    libraries.Add(path);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("RustProcessService", $"Failed to parse '{vdfPath}'.", ex);
        }

        return libraries;
    }

    /// <summary>
    /// Extracts a single "key" "value" pair from a flat VDF/KeyValues file. Sufficient for the two
    /// narrow, well-known schemas used here (libraryfolders.vdf's "path" entries, appmanifest's
    /// "installdir") - a full nested-brace tokenizer isn't warranted for just these lookups.
    /// </summary>
    private static string? ExtractVdfValue(string content, string key)
    {
        Match match = Regex.Match(content, $"\"{Regex.Escape(key)}\"\\s*\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }
}
