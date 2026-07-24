using System.Collections.Generic;

namespace RustOptimizer.Interface;

/// <summary>
/// A user-saved graphics profile: a display name plus a chosen tier for each Graphics-page quality
/// slider, keyed by the slider's stable <c>PreviewId</c> to the picked tier's <c>PreviewId</c> (e.g.
/// "ShadowQuality" → "High"). Storing tier ids rather than raw convar values keeps a profile compact
/// and tied to the sliders the user actually sees. The three built-in presets (Low End PC,
/// Competitive, Cinematic) live in code, not here, so they can't be overridden or deleted.
/// </summary>
public sealed class GraphicsProfile
{
    /// <summary>The user-visible profile name, shown in the Graphics page's profile dropdown.</summary>
    public string Name { get; set; } = "";

    /// <summary>The chosen tier per slider: slider <c>PreviewId</c> → tier <c>PreviewId</c>.</summary>
    public Dictionary<string, string> Sliders { get; set; } = [];
}

/// <summary>
/// How throughput figures are displayed. Networking hardware is advertised in bits (a "1 Gbit"
/// connection), while file sizes and transfer tools use bytes - so neither unit is universally
/// right and the choice is left to the user.
/// </summary>
public enum ThroughputUnit
{
    /// <summary>Bytes per second, e.g. "1.2 MB/s".</summary>
    Bytes,
    /// <summary>Bits per second, e.g. "9.6 Mbps".</summary>
    Bits
}

/// <summary>
/// Every persisted application preference, serialized as one JSON document. Properties carry their
/// own defaults so a missing or partial file still produces a sensible configuration rather than
/// zeroes and nulls.
/// </summary>
public sealed class AppSettings
{
    /// <summary>The selected colour theme.</summary>
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>
    /// The selected interface language, or <see langword="null"/> if the user has never picked one -
    /// in which case it's detected from the Windows UI culture instead of defaulting to English.
    /// </summary>
    public AppLanguage? Language { get; set; }

    /// <summary>Whether the app registers itself to launch when Windows starts.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Whether the app looks for a newer release each time it opens.</summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Whether an available update is downloaded and applied without asking. Off by default -
    /// applying an update replaces the running app and restarts it, which shouldn't happen to
    /// someone who never opted in.
    /// </summary>
    public bool AutoUpdate { get; set; }

    /// <summary>Which unit network throughput is displayed in.</summary>
    public ThroughputUnit ThroughputUnit { get; set; } = ThroughputUnit.Bytes;

    /// <summary>How many days of log files to keep before older ones are pruned.</summary>
    public int LogRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether every log level is written, including Debug. Off by default - normal runs log at
    /// Info and above. Turned on when diagnosing a problem for someone, so their log captures
    /// everything rather than only what was deemed noteworthy in advance.
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// The user's saved custom graphics profiles, managed from the Graphics page. The built-in
    /// presets aren't included - they're defined in code and always available.
    /// </summary>
    public List<GraphicsProfile> GraphicsProfiles { get; set; } = [];
}

/// <summary>
/// Loads and persists <see cref="AppSettings"/> as a single JSON file in the app's per-user data
/// folder. Replaces the previous one-file-per-preference approach (theme.tsgs, language.tsgs),
/// which needed a new service, file and load/save pair for every setting added.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>The current settings. Mutated in place by callers, then persisted via <see cref="Save"/>.</summary>
    AppSettings Current { get; }

    /// <summary>
    /// Loads the settings file, falling back to defaults if it's missing or unreadable. Must run
    /// before anything reads <see cref="Current"/> - notably before the theme and language services
    /// initialize, since both now source their value from here.
    /// </summary>
    void Initialize();

    /// <summary>Writes <see cref="Current"/> back to disk. Failures are logged, never thrown.</summary>
    void Save();
}