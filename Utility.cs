using System.Diagnostics;
using System.IO;
using System;

namespace RustOptimizer;

/// <summary>
/// Cross-platform helpers shared across the application.
/// </summary>
public static class Utility
{
    /// <summary>
    /// Gets the user-facing application version string, including nightly or prerelease suffixes when present.
    /// </summary>
    /// <returns>The file version in debug builds; otherwise the product version with a file-version fallback.</returns>
    public static string GetDisplayVersion()
    {
        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(GetExePath());
#if DEBUG
        return versionInfo.FileVersion ?? versionInfo.ProductVersion ?? "N/A";
#else
        return versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "N/A";
#endif
    }

    /// <summary>
    /// Gets the full path to the running application executable.
    /// </summary>
    public static string GetExePath()
    {
        return Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, $"{Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0])}.exe");
    }
}