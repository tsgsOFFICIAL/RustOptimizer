namespace RustOptimizer.Interface;

public enum ConfigPreset
{
    LowEndPc,
    Competitive,
    Streamer,
    Cinematic
}

/// <summary>
/// Applies preset graphics/performance settings to Rust's client.cfg.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Applies a preset's convar values to Rust's client.cfg, leaving every other setting
    /// untouched. Backs up the existing file to client.cfg.bak first. Returns <see langword="false"/>
    /// (without writing anything) if Rust is currently running, not installed, or client.cfg is missing.
    /// </summary>
    bool ApplyPreset(ConfigPreset preset);
}