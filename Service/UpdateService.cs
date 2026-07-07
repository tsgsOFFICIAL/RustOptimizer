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
/// Checks GitHub Releases for a newer version and applies it once downloaded. Installer-managed
/// installs (detected via the uninstaller Inno Setup always drops next to the exe) are updated by
/// silently re-running the new Setup.exe, which keeps the registered Add/Remove Programs version in
/// sync. Portable zip installs are updated by swapping the extracted files in over the running app.
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

        string assetSuffix = IsInstallerInstall()
            ? "-Setup.exe"
            : IsSelfContained() ? "-self-contained.zip" : "-framework-dependent.zip";

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

    public Task ApplyUpdateAsync(UpdateInfo update, CancellationToken ct = default)
        => update.AssetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? ApplyInstallerUpdateAsync(update, ct)
            : ApplyPortableUpdateAsync(update, ct);

    /// <summary>
    /// Downloads the new Setup.exe and re-runs it silently. Inno Setup's Restart Manager integration
    /// (CloseApplications/RestartApplications) closes the running app, replaces its files, updates the
    /// registered uninstall entry's version, and relaunches - all without a UAC prompt since the install
    /// itself is per-user.
    /// </summary>
    private async Task ApplyInstallerUpdateAsync(UpdateInfo update, CancellationToken ct)
    {
        string installerPath = Path.Combine(Path.GetTempPath(), update.AssetName);
        await DownloadToFileAsync(update.DownloadUrl, installerPath, ct);

        Process.Start(new ProcessStartInfo(installerPath,
            "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS")
        {
            UseShellExecute = false
        });

        Environment.Exit(0);
    }

    /// <summary>
    /// Downloads the matching zip, extracts it to a staging folder, then hands off to a short PowerShell
    /// script that waits for this process to exit, copies the new files over the install directory, and
    /// relaunches - a separate process is required since the running exe holds its own files locked.
    /// </summary>
    private async Task ApplyPortableUpdateAsync(UpdateInfo update, CancellationToken ct)
    {
        string exePath = Utility.GetExePath();
        string installDir = Path.GetDirectoryName(exePath)
            ?? throw new InvalidOperationException("Could not determine the install directory.");

        string stagingDir = Path.Combine(Path.GetTempPath(), "RustOptimizer-Update", update.Version);
        Directory.CreateDirectory(stagingDir);

        string zipPath = Path.Combine(Path.GetTempPath(), update.AssetName);
        await DownloadToFileAsync(update.DownloadUrl, zipPath, ct);

        ZipFile.ExtractToDirectory(zipPath, stagingDir, overwriteFiles: true);
        File.Delete(zipPath);

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

    private static async Task DownloadToFileAsync(string url, string path, CancellationToken ct)
    {
        await using Stream download = await Client.GetStreamAsync(url, ct);
        await using FileStream file = File.Create(path);
        await download.CopyToAsync(file, ct);
    }

    /// <summary>
    /// Inno Setup always drops its uninstaller next to the exe, regardless of install location -
    /// portable zip extracts never have one, so its presence tells the two deployment forms apart.
    /// </summary>
    private static bool IsInstallerInstall()
        => File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

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
