using RustOptimizer.Service.Logging;
using System.Diagnostics;
using System.Reflection;
using System.IO;
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
    /// <returns>The product version (includes nightly/prerelease suffixes), falling back to the plain file version.</returns>
    public static string GetDisplayVersion()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
        // ProductVersion (InformationalVersion) carries the nightly commit-hash suffix; FileVersion
        // is strictly numeric and never has it. See .github/workflows/nightly.yml.
        return versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "N/A";
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
    /// Gets a marketing-style Windows name (e.g. "Windows 11"). Windows 11 reports the same major
    /// version (10) as Windows 10; only the build number (&gt;= 22000) actually distinguishes them.
    /// </summary>
    public static string GetFriendlyOsName()
    {
        Version v = Environment.OSVersion.Version;
        return v is { Major: 10, Build: >= 22000 } ? "Windows 11" : v.Major == 10 ? "Windows 10" : $"Windows NT {v}";
    }

    /// <summary>
    /// Identifies traffic this app sends to a site, so the destination can attribute visitors to
    /// Rust Optimizer rather than seeing them as direct. Standard UTM parameters, since that's what
    /// analytics tools already understand.
    /// </summary>
    private const string ReferralQuery = "utm_source=RustOptimizer&utm_medium=app";

    /// <summary>
    /// Opens the given URL using the operating system's default handler, tagging web links with
    /// <see cref="ReferralQuery"/> first. Fails silently (e.g. no default browser configured) so a
    /// broken link never crashes the app.
    /// </summary>
    public static void OpenUrl(string url)
    {
        string target = AddReferral(url);

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Warn("Utility", $"Failed to open URL '{target}'.", ex);
        }
    }

    /// <summary>
    /// Appends the referral parameters to a web address, preserving any query it already has.
    /// <para>
    /// Only http/https are touched. This same method opens <c>steam://</c> protocol links and local
    /// folder paths (see <see cref="Logging.AppLog.OpenLogDirectory"/>), and a query string appended
    /// to either of those would simply break them. Anything already carrying a <c>utm_source</c> is
    /// left alone so a hand-tagged link doesn't end up with two.
    /// </para>
    /// </summary>
    private static string AddReferral(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return url;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return url;

            if (url.Contains("utm_source=", StringComparison.OrdinalIgnoreCase))
                return url;

            // UriBuilder rather than string concatenation: it puts the query before any #fragment,
            // which a naive append would corrupt.
            UriBuilder builder = new(uri);
            builder.Query = string.IsNullOrEmpty(builder.Query)
                ? ReferralQuery
                : $"{builder.Query.TrimStart('?')}&{ReferralQuery}";

            return builder.Uri.ToString();
        }
        catch (Exception ex)
        {
            AppLog.Warn("Utility", $"Failed to tag '{url}' with referral parameters; opening it unchanged.", ex);
            return url;
        }
    }
}