using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net;
using System;

namespace RustOptimizer;

/// <summary>
/// Fetches the changelog directly from GitHub so an update check can show the user what changed in
/// the version(s) they're about to install, without waiting for a new build to embed it locally.
/// </summary>
public static class RemoteChangelog
{
    private const string RawBaseUrl = "https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/";

    private static readonly HttpClient Client = CreateClient();

    /// <summary>
    /// Fetches the changelog and returns only the sections newer than <paramref name="installedVersion"/>,
    /// merged with an English fallback for any version the current language hasn't been translated for yet.
    /// </summary>
    /// <param name="language">The language to prefer for the changelog text.</param>
    /// <param name="installedVersion">The currently installed version (e.g. from <see cref="Utility.GetDisplayVersion"/>).</param>
    /// <param name="ct">A token to cancel the fetch.</param>
    /// <exception cref="HttpRequestException">The English baseline fetch failed for a reason other than a missing locale file.</exception>
    /// <exception cref="TaskCanceledException">The fetch timed out or was canceled.</exception>
    public static async Task<string> GetChangesSinceAsync(AppLanguage language, string installedVersion, CancellationToken ct = default)
    {
        // The English file is the mandatory baseline: if it fails to fetch, there's nothing to show,
        // so that failure is allowed to propagate to the caller (e.g. to skip the changelog step).
        string english = await FetchAsync("CHANGELOG.md", ct);

        string localized = english;
        if (language != AppLanguage.English)
        {
            try
            {
                localized = await FetchAsync(LocaleFileName(language), ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Translated file hasn't been published yet - degrade to English-only, same as a
                // missing embedded locale resource. Any other failure still propagates.
                localized = english;
            }
        }

        return ChangelogParser.MergeAndExtractSince(localized, english, installedVersion);
    }

    private static Task<string> FetchAsync(string fileName, CancellationToken ct)
        => Client.GetStringAsync(RawBaseUrl + fileName, ct);

    /// <summary>
    /// Maps an application language to its changelog file name. Mirrors
    /// <c>LocalizationService</c>'s private locale-file-name mapping.
    /// </summary>
    private static string LocaleFileName(AppLanguage language) => language switch
    {
        AppLanguage.Danish => "CHANGELOG.da-DK.md",
        AppLanguage.Russian => "CHANGELOG.ru-RU.md",
        _ => "CHANGELOG.md"
    };

    private static HttpClient CreateClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("User-Agent", "RustOptimizer");
        return client;
    }
}