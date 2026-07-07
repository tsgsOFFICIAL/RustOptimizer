using System.Diagnostics;
using System.IO;
using System.Reflection;
using System;

namespace RustOptimizer;

/// <summary>
/// The project's external links, shared by anywhere in the UI that offers to open them
/// (the About page, the main window's footer).
/// </summary>
public static class ProjectLinks
{
    public const string GitHub = "https://github.com/tsgsOFFICIAL/RustOptimizer";
    public const string Discord = "https://discord.gg/Cddu5aJ";
    public const string KoFi = "https://ko-fi.com/tsgsOFFICIAL";
}

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
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
        return versionInfo.FileVersion ?? versionInfo.ProductVersion ?? "N/A";
    }

    /// <summary>
    /// Gets the full path to the running application executable.
    /// </summary>
    public static string GetExePath()
    {
        return Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, $"{Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0])}.exe");
    }

    /// <summary>
    /// Opens the given URL using the operating system's default handler. Fails silently (e.g. no
    /// default browser configured) so a broken link never crashes the app.
    /// </summary>
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}