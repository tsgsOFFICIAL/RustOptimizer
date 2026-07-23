using RustOptimizer.Service.Logging;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Registers the app to launch when Windows starts, via the current user's Run key. HKCU rather
/// than HKLM deliberately - a per-user entry needs no administrator rights, so the toggle never
/// raises a UAC prompt.
/// </summary>
[SupportedOSPlatform("windows")]
public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "RustOptimizer";

    /// <summary>
    /// Whether the app is currently registered to start with Windows. Reads the registry rather
    /// than trusting the saved setting, so a value removed by another tool is reflected honestly.
    /// </summary>
    public static bool IsRegistered()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string path && !string.IsNullOrWhiteSpace(path);
        }
        catch (Exception ex)
        {
            AppLog.Warn("StartupRegistration", "Failed to read the startup registration.", ex);
            return false;
        }
    }

    /// <summary>
    /// Adds or removes the startup entry. Returns whether the change was actually applied, so the
    /// UI can fall back to the real state instead of showing a toggle that silently did nothing.
    /// </summary>
    public static bool SetRegistered(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (enabled)
                // Quoted: the path contains spaces on any normal install, and Windows would
                // otherwise treat everything after the first space as arguments.
                key.SetValue(ValueName, $"\"{Utility.GetExePath()}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);

            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("StartupRegistration", $"Failed to {(enabled ? "add" : "remove")} the startup registration.", ex);
            return false;
        }
    }
}