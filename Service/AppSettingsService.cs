using RustOptimizer.Service.Logging;
using RustOptimizer.Interface;
using System.Text.Json;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IAppSettingsService" />
public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public void Initialize()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                AppLog.Debug("AppSettingsService", "No settings file found; starting from defaults.");
                return;
            }

            // A file that exists but won't parse leaves defaults in place rather than throwing -
            // a corrupt preference file shouldn't stop the app from opening.
            Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            AppLog.Debug("AppSettingsService", $"Loaded settings from '{SettingsPath}': theme={Current.Theme}, language={Current.Language?.ToString() ?? "auto"}, " +
                $"startWithWindows={Current.StartWithWindows}, checkUpdates={Current.CheckForUpdatesOnStartup}, autoUpdate={Current.AutoUpdate}, " +
                $"throughputUnit={Current.ThroughputUnit}, logRetentionDays={Current.LogRetentionDays}, verbose={Current.VerboseLogging}.");
        }
        catch (Exception ex)
        {
            AppLog.Warn("AppSettingsService", $"Failed to load settings from '{SettingsPath}'; using defaults.", ex);
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, SerializerOptions));
            AppLog.Debug("AppSettingsService", $"Saved settings to '{SettingsPath}'.");
        }
        catch (Exception ex)
        {
            AppLog.Warn("AppSettingsService", $"Failed to save settings to '{SettingsPath}'.", ex);
        }
    }
}