namespace RustOptimizer.Interface;

/// <summary>
/// Detects whether Rust's game process is running, and launches it through Steam.
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
}
