namespace RustOptimizer.Interface;

/// <summary>
/// Detects whether Rust's game process is running, launches it through Steam, and resolves
/// Rust's install directory on disk by reading Steam's registry entry and library manifests.
/// </summary>
public interface IRustProcessService
{
    /// <summary>
    /// Returns whether Rust's game process is currently running.
    /// </summary>
    bool IsRunning();

    /// <summary>
    /// Launches Rust through Steam's protocol handler.
    /// </summary>
    void Launch();

    /// <summary>
    /// Verified Rust's game files through Steam's protocol handler.
    /// </summary>
    void VerifyFiles();

    /// <summary>
    /// Resolves Rust's install directory on disk by locating Steam via the registry, enumerating
    /// every configured Steam library folder, and checking each for Rust's app manifest.
    /// Returns <see langword="null"/> if Steam's registry entry is missing, no library contains
    /// Rust's manifest, or the resolved folder doesn't actually exist on disk.
    /// </summary>
    string? GetInstallPath();
}
