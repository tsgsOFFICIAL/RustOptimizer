using System.Net.Http.Headers;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.IO.Compression;
using System.Text.Json;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// Checks GitHub Releases for a newer version and swaps it in over the current install once downloaded.
/// Only supports portable (zip) installs: it overwrites files in the running app's own directory, which
/// requires no elevation for a portable copy but will fail against a Program Files installer deployment.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private const string Owner = "tsgsOFFICIAL";
    private const string Repo = "RustOptimizer";

    private static readonly HttpClient Client = CreateClient();

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest", ct);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        string tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
        string latestVersion = tagName.TrimStart('v');

        if (!TryParseCore(latestVersion, out Version latest) ||
            !TryParseCore(Utility.GetDisplayVersion(), out Version current) ||
            latest <= current)
        {
            return null;
        }

        string assetSuffix = IsSelfContained() ? "-self-contained.zip" : "-framework-dependent.zip";
        foreach (JsonElement asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            if (!name.EndsWith(assetSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            string url = asset.GetProperty("browser_download_url").GetString()
                ?? throw new InvalidOperationException($"Release asset '{name}' has no download URL.");
            return new UpdateInfo(latestVersion, url, name);
        }

        return null;
    }

    public async Task ApplyUpdateAsync(UpdateInfo update, CancellationToken ct = default)
    {
        string exePath = Utility.GetExePath();
        string installDir = Path.GetDirectoryName(exePath)
            ?? throw new InvalidOperationException("Could not determine the install directory.");

        string stagingDir = Path.Combine(Path.GetTempPath(), "RustOptimizer-Update", update.Version);
        Directory.CreateDirectory(stagingDir);

        string zipPath = Path.Combine(Path.GetTempPath(), update.AssetName);
        await using (Stream download = await Client.GetStreamAsync(update.DownloadUrl, ct))
        await using (FileStream file = File.Create(zipPath))
        {
            await download.CopyToAsync(file, ct);
        }

        ZipFile.ExtractToDirectory(zipPath, stagingDir, overwriteFiles: true);
        File.Delete(zipPath);

        // The running exe holds its own files locked, so the copy-over-and-relaunch has to happen from a
        // separate process that outlives this one - a short PowerShell script is enough, no helper exe needed.
        string scriptPath = Path.Combine(Path.GetTempPath(), $"RustOptimizer-Update-{update.Version}.ps1");
        string script = $"""
            Wait-Process -Id {Environment.ProcessId} -ErrorAction SilentlyContinue
            Copy-Item -Path "{stagingDir}\*" -Destination "{installDir}" -Recurse -Force
            Remove-Item -Path "{stagingDir}" -Recurse -Force
            Start-Process -FilePath "{exePath}"
            Remove-Item -Path "{scriptPath}" -Force
            """;
        await File.WriteAllTextAsync(scriptPath, script, ct);

        Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\"")
        {
            UseShellExecute = false
        });

        Environment.Exit(0);
    }

    /// <summary>
    /// Self-contained publishes bundle the runtime (including this assembly) next to the exe;
    /// framework-dependent ones rely on a system-installed .NET runtime instead.
    /// </summary>
    private static bool IsSelfContained()
        => File.Exists(Path.Combine(AppContext.BaseDirectory, "System.Private.CoreLib.dll"));

    private static bool TryParseCore(string version, out Version result)
    {
        int dash = version.IndexOf('-');
        string core = dash >= 0 ? version[..dash] : version;
        return Version.TryParse(core, out result!);
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RustOptimizer", null));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
