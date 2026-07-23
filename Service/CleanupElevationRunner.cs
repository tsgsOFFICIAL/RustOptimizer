using RustOptimizer.Service.Logging;
using System.Runtime.Versioning;
using System.Text.Json;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <summary>
/// The result of an elevated cleanup run, handed back to the unelevated UI process through
/// <see cref="CleanupElevationRunner.ResultFilePath"/>. <see cref="ElevationHelper"/> can't capture
/// the elevated child's stdout (<c>runas</c> forces <c>UseShellExecute</c>), and the exit code is a
/// single integer, so a small JSON file is the only channel that can carry both numbers back.
/// </summary>
/// <param name="BytesFreed">Total bytes reclaimed by the admin-only targets.</param>
/// <param name="FilesSkipped">How many files the elevated run still couldn't delete.</param>
public sealed record CleanupElevationResult(long BytesFreed, int FilesSkipped);

/// <summary>
/// The elevated codepath for <c>--clean-system-files</c>, intercepted in <c>Program.Main</c> before
/// the normal DI/Avalonia startup runs. Clears every target that needs administrator rights in one
/// pass, so the user sees a single UAC prompt rather than one per target, then writes its totals to
/// <see cref="ResultFilePath"/> for the parent process to read and delete.
/// </summary>
[SupportedOSPlatform("windows")]
public static class CleanupElevationRunner
{
    /// <summary>The command-line argument that selects this codepath.</summary>
    public const string Argument = "--clean-system-files";

    /// <summary>
    /// Where the elevated run leaves its results. Sits in the app's own %APPDATA% folder rather than
    /// %TEMP% specifically because a cleanup run empties %TEMP%. <c>runas</c> elevates the same user
    /// account, so this resolves to the same path in both processes.
    /// </summary>
    public static string ResultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "cleanup-result.json");

    /// <summary>
    /// Clears every admin-only target and writes the totals to <see cref="ResultFilePath"/>. Returns
    /// the process exit code: 0 if the run completed, 1 if it threw outright. Individual targets
    /// failing doesn't fail the run - a locked file is the normal case, not an error.
    /// </summary>
    public static int Run()
    {
        CleanupTally tally = new();

        try
        {
            ClearSystemTemp(tally);
            ClearCrashDumps(tally);
            ClearWindowsUpdateLeftovers(tally);
            ClearChkdskFragments(tally);
        }
        catch (Exception ex)
        {
            AppLog.Warn("CleanupElevationRunner", "The elevated cleanup run failed.", ex);
            WriteResult(tally);
            return 1;
        }

        AppLog.Info("CleanupElevationRunner", $"Elevated cleanup freed {tally.BytesFreed} bytes, skipped {tally.FilesSkipped} files.");
        WriteResult(tally);
        return 0;
    }

    /// <summary>Clears C:\Windows\Temp, applying the same 24-hour guard the per-user temp folder gets.</summary>
    private static void ClearSystemTemp(CleanupTally tally)
        => CleanupService.DeleteContents(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            tally,
            TimeSpan.FromHours(24));

    /// <summary>
    /// Deletes the kernel memory dump and every minidump. MEMORY.DMP is sized to physical RAM, so
    /// this single file is routinely the largest thing a cleanup run removes.
    /// </summary>
    private static void ClearCrashDumps(CleanupTally tally)
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        CleanupService.DeleteFileByPath(Path.Combine(windows, "MEMORY.DMP"), tally);
        CleanupService.DeleteContents(Path.Combine(windows, "Minidump"), tally);
    }

    /// <summary>
    /// Clears downloaded Windows Update packages and Delivery Optimization's P2P chunks.
    /// <para>
    /// Deliberately does <em>not</em> stop wuauserv/dosvc first. Stopping them is the recipe for
    /// renaming the whole SoftwareDistribution folder, which isn't what this does - and it cost
    /// ~20 seconds on every run whether or not there was anything to delete, while risking the
    /// worst outcome this feature has: leaving Windows Update stopped. Anything the services still
    /// hold open is simply counted as skipped and cleared on a later run instead.
    /// </para>
    /// </summary>
    private static void ClearWindowsUpdateLeftovers(CleanupTally tally)
    {
        string softwareDistribution = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution");

        CleanupService.DeleteContents(Path.Combine(softwareDistribution, "Download"), tally);

        // Created on demand rather than always present - absent is normal, not a wrong path.
        CleanupService.DeleteContents(Path.Combine(softwareDistribution, "DeliveryOptimization"), tally);
    }

    /// <summary>
    /// Deletes chkdsk's recovered fragments. Only ever present when chkdsk was run with /f or /r
    /// <em>and</em> found orphaned chains it couldn't relink, which on a healthy NTFS volume is
    /// close to never - included for completeness rather than yield.
    /// </summary>
    private static void ClearChkdskFragments(CleanupTally tally)
    {
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";

        try
        {
            foreach (string file in Directory.GetFiles(root, "*.chk"))
                CleanupService.DeleteFileByPath(file, tally);

            // FOUND.000, FOUND.001, ... one per chkdsk run that recovered anything. Hidden + system,
            // so they never show up in a casual directory listing.
            foreach (string directory in Directory.GetDirectories(root, "FOUND.*"))
            {
                CleanupService.DeleteContents(directory, tally);
                CleanupService.DeleteDirectoryIfEmpty(directory, tally);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Warn("CleanupElevationRunner", "Failed to enumerate chkdsk fragments.", ex);
        }
    }

    /// <summary>
    /// Writes the run's totals for the parent process. Failing to write isn't fatal to the cleanup
    /// itself - the files are already gone - so the parent just reports nothing reclaimed.
    /// </summary>
    private static void WriteResult(CleanupTally tally)
    {
        try
        {
            string? directory = Path.GetDirectoryName(ResultFilePath);
            if (directory != null)
                Directory.CreateDirectory(directory);

            CleanupElevationResult result = new(tally.BytesFreed, tally.FilesSkipped);
            File.WriteAllText(ResultFilePath, JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            AppLog.Warn("CleanupElevationRunner", "Failed to write the elevated cleanup result file.", ex);
        }
    }
}