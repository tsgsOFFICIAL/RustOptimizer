using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="ISystemTweaksService" />
/// <remarks>
/// The active power plan is switched via <c>powercfg.exe</c> rather than the WMI
/// <c>Win32_PowerPlan</c> class, which is blocked in Terminal Services/RDP sessions. Everything
/// else here is a plain HKCU registry value - no admin rights needed anywhere in this class.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class SystemTweaksService(IRustProcessService rustProcess) : ISystemTweaksService
{
    private const string MouseKeyPath = @"Control Panel\Mouse";
    private const string GameBarKeyPath = @"Software\Microsoft\GameBar";
    private const string GameConfigStoreKeyPath = @"System\GameConfigStore";
    private const string GameDvrKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string AppCompatFlagsLayersKeyPath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

    // Medal's background encoder resets AutoGameModeEnabled/AllowAutoGameMode itself shortly
    // after launch - checked under both names since either the encoder or the main UI process
    // being up means it's active.
    private static readonly string[] MedalProcessNames = ["MedalEncoder", "Medal"];

    // The flag Windows writes when you check "Disable fullscreen optimizations" on an exe's
    // Properties > Compatibility tab - applied to Rust specifically instead of by hand.
    private const string DisableFullscreenOptimizationsFlag = "~ DISABLEDXMAXIMIZEDWINDOWEDMODE";

    private const uint SPI_SETMOUSE = 0x0004;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);

    /// <inheritdoc />
    public IReadOnlyList<PowerPlanInfo> GetPowerPlans()
    {
        List<PowerPlanInfo> plans = [];
        try
        {
            (int exitCode, string output) = RunPowerCfg("/list");
            if (exitCode != 0)
                return plans;

            foreach (Match match in PowerPlanLineRegex().Matches(output))
            {
                string id = match.Groups[1].Value;
                string name = match.Groups[2].Value.Trim();
                bool isActive = match.Groups[3].Success;
                plans.Add(new PowerPlanInfo(id, name, isActive));
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", "Failed to list power plans via powercfg.", ex);
        }

        return plans;
    }

    /// <inheritdoc />
    public bool SetActivePowerPlan(string planId)
    {
        try
        {
            (int exitCode, _) = RunPowerCfg($"/setactive {planId}");
            return exitCode == 0;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", "Failed to activate power plan via powercfg.", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public GamingTweaksSettings GetGamingTweaksSettings() => new(
        PointerPrecisionEnabled: ReadString(MouseKeyPath, "MouseSpeed", defaultValue: "1") != "0",
        GameModeEnabled: ReadDword(GameBarKeyPath, "AutoGameModeEnabled", defaultValue: 1) != 0,
        // GameDVR_Enabled is the master switch, HistoricalCaptureEnabled the separate instant-replay
        // buffer under a different key - reported "on" only when both are.
        BackgroundRecordingEnabled: ReadDword(GameConfigStoreKeyPath, "GameDVR_Enabled", defaultValue: 1) != 0
            && ReadDword(GameDvrKeyPath, "HistoricalCaptureEnabled", defaultValue: 1) != 0,
        FullscreenOptimizationsDisabledForRust: GetRustExePath() is { } exePath ? ReadFullscreenOptimizationsFlag(exePath) : null,
        MedalRunning: IsMedalRunning());

    /// <inheritdoc />
    public bool SetPointerPrecisionEnabled(bool enabled)
    {
        // Mouse acceleration isn't purely registry-backed - Windows keeps a live copy in the
        // desktop session that only refreshes via SystemParametersInfo. SPIF_UPDATEINIFILE writes
        // the same registry values the Mouse Properties dialog does; SPIF_SENDCHANGE broadcasts
        // WM_SETTINGCHANGE so it applies immediately instead of after next login.
        int[] mouseParams = enabled ? [6, 10, 1] : [0, 0, 0];
        bool success = SystemParametersInfo(SPI_SETMOUSE, 0, mouseParams, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        if (!success)
            AppLog.Warn("SystemTweaksService", $"SystemParametersInfo(SPI_SETMOUSE) failed. Win32 error: {Marshal.GetLastWin32Error()}");

        return success;
    }

    /// <inheritdoc />
    public bool SetGameModeEnabled(bool enabled) => WriteDword(GameBarKeyPath, "AutoGameModeEnabled", enabled ? 1 : 0);

    /// <inheritdoc />
    public bool SetBackgroundRecordingEnabled(bool enabled)
    {
        bool masterSwitch = WriteDword(GameConfigStoreKeyPath, "GameDVR_Enabled", enabled ? 1 : 0);
        bool instantReplayBuffer = WriteDword(GameDvrKeyPath, "HistoricalCaptureEnabled", enabled ? 1 : 0);
        return masterSwitch && instantReplayBuffer;
    }

    /// <inheritdoc />
    public bool SetFullscreenOptimizationsDisabledForRust(bool disabled)
    {
        string? exePath = GetRustExePath();
        if (exePath is null)
            return false;

        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(AppCompatFlagsLayersKeyPath, writable: true);

            // DisableFullscreenOptimizationsFlag itself contains a space ("~ DISABLEDXMAXIMIZEDWINDOWEDMODE"
            // is one token), so it can't be found by splitting the raw value on every space - only
            // touch our one flag, in case something else already set others.
            string raw = (key.GetValue(exePath) as string ?? "").Trim();
            bool hasFlag = raw.Contains(DisableFullscreenOptimizationsFlag, StringComparison.Ordinal);

            string updated = raw;
            if (disabled && !hasFlag)
                updated = raw.Length == 0 ? DisableFullscreenOptimizationsFlag : $"{raw} {DisableFullscreenOptimizationsFlag}";
            else if (!disabled && hasFlag)
                updated = raw.Replace(DisableFullscreenOptimizationsFlag, "").Trim();

            if (updated.Length == 0)
                key.DeleteValue(exePath, throwOnMissingValue: false);
            else
                key.SetValue(exePath, updated, RegistryValueKind.String);

            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", "Failed to set fullscreen-optimizations flag for Rust.", ex);
            return false;
        }
    }

    /// <summary>Reads whether the fullscreen-optimizations-disabled flag is set for the given exe.</summary>
    private static bool? ReadFullscreenOptimizationsFlag(string exePath)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppCompatFlagsLayersKeyPath);
            string raw = key?.GetValue(exePath) as string ?? "";
            return raw.Contains(DisableFullscreenOptimizationsFlag, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", "Failed to read fullscreen-optimizations flag for Rust.", ex);
            return null;
        }
    }

    /// <summary>Whether Medal's clip recorder is currently running.</summary>
    private static bool IsMedalRunning()
    {
        foreach (string name in MedalProcessNames)
        {
            Process[] processes = Process.GetProcessesByName(name);
            foreach (Process process in processes)
                process.Dispose();

            if (processes.Length > 0)
                return true;
        }

        return false;
    }

    /// <summary>Resolves Rust's exe path from its install directory, or <see langword="null"/> if Rust isn't installed.</summary>
    private string? GetRustExePath()
    {
        string? installDir = rustProcess.GetInstallPath();
        return installDir != null ? Path.Combine(installDir, "RustClient.exe") : null;
    }

    /// <summary>Reads a DWORD registry value, or <paramref name="defaultValue"/> if it's missing or unreadable.</summary>
    private static int ReadDword(string keyPath, string valueName, int defaultValue)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath);
            return key?.GetValue(valueName) is int value ? value : defaultValue;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", $"Failed to read '{valueName}' from the registry.", ex);
            return defaultValue;
        }
    }

    /// <summary>Reads a string registry value, or <paramref name="defaultValue"/> if it's missing or unreadable.</summary>
    private static string ReadString(string keyPath, string valueName, string defaultValue)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath);
            return key?.GetValue(valueName) as string ?? defaultValue;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", $"Failed to read '{valueName}' from the registry.", ex);
            return defaultValue;
        }
    }

    /// <summary>Writes a DWORD registry value, creating the key if needed. Returns whether it succeeded.</summary>
    private static bool WriteDword(string keyPath, string valueName, int value)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key.SetValue(valueName, value, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemTweaksService", $"Failed to write '{valueName}' to the registry.", ex);
            return false;
        }
    }

    /// <summary>Runs <c>powercfg.exe</c> with the given arguments and captures its exit code and stdout.</summary>
    private static (int ExitCode, string StandardOutput) RunPowerCfg(string arguments)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("powercfg.exe", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    /// <summary>Matches one "Power Scheme GUID: {guid} (Name) [*]" line from <c>powercfg /list</c>.</summary>
    [GeneratedRegex(@"Power Scheme GUID:\s*([0-9a-fA-F-]{36})\s*\(([^)]*)\)\s*(\*)?")]
    private static partial Regex PowerPlanLineRegex();
}