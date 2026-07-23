using System.Runtime.InteropServices;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.IO;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="ICleanupService" />
[SupportedOSPlatform("windows")]
public sealed class CleanupService : ICleanupService
{
    private const string SteamAppId = "252490";

    // Anything written in the last day may belong to a running installer, extractor or game that
    // hasn't finished with it yet. Only the general-purpose temp roots get this guard - the cache
    // and log targets below are all owned by software that recreates them on demand.
    private static readonly TimeSpan MinimumTempFileAge = TimeSpan.FromHours(24);

    private readonly IRustProcessService _rustProcess;

    /// <summary>Creates the service. <paramref name="rustProcess"/> supplies Steam/Rust paths and running-state checks.</summary>
    public CleanupService(IRustProcessService rustProcess) => _rustProcess = rustProcess;

    /// <summary>
    /// Orchestrates the run. Deliberately async all the way down: the elevated child is awaited
    /// rather than waited on, because blocking a thread-pool thread for the child's whole lifetime
    /// while <see cref="Parallel"/> is also asking the pool for workers starves it, and the pool
    /// only injects replacement threads about once a second.
    /// </summary>
    public async Task<CleanupOutcome> CleanAsync(CleanupOptions options, IProgress<CleanupProgress>? progress, CancellationToken cancellationToken)
    {
        CleanupTally tally = new();
        bool rustRunning = _rustProcess.IsRunning();

        // Started first, not last: the elevated child is a separate process doing independent work,
        // so running it alongside the unelevated pass hides its cost almost entirely. It also puts
        // the UAC prompt on screen immediately rather than after a silent delay.
        Task<bool>? elevatedTask = options.IncludeSystemFiles
            ? Task.Run(() => RunSystemTargetsElevated(tally), CancellationToken.None)
            : null;

        List<(string LabelKey, Action Run)> steps = BuildSteps(options, tally, rustRunning);

        // The elevated wait is one extra step for progress purposes, but it isn't work this side
        // performs, so it isn't in the list the worker iterates.
        int totalSteps = steps.Count + (elevatedTask != null ? 1 : 0);

        bool cancelled = await Task.Run(() => RunSteps(steps, totalSteps, progress, cancellationToken), CancellationToken.None);

        bool elevationDeclined = false;
        if (elevatedTask != null)
        {
            // Awaited even after a cancel - it's a separate process that can't be stopped from here,
            // and abandoning it would lose both its result file and the bytes it freed.
            progress?.Report(new CleanupProgress("ClearCacheStepSystemFiles", steps.Count, totalSteps));
            elevationDeclined = !await elevatedTask;
        }

        progress?.Report(new CleanupProgress("", totalSteps, totalSteps));

        AppLog.Info("CleanupService", $"Cleanup finished: {tally.BytesFreed} bytes freed in total, {tally.FilesSkipped} files skipped, cancelled={cancelled}.");
        return new CleanupOutcome(tally.BytesFreed, tally.FilesSkipped, rustRunning, elevationDeclined, cancelled);
    }

    /// <summary>
    /// Runs each group in order on a worker thread, reporting progress as it goes and timing each
    /// one into the log. Returns whether the run was cancelled part-way. Cancellation is checked
    /// between groups so a stopped run leaves a coherent partial result, not a half-deleted tree.
    /// </summary>
    private static bool RunSteps(
        List<(string LabelKey, Action Run)> steps, int totalSteps, IProgress<CleanupProgress>? progress, CancellationToken cancellationToken)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                return true;

            progress?.Report(new CleanupProgress(steps[i].LabelKey, i, totalSteps));

            // Per-group timings, so a slow run can be diagnosed from the log rather than guessed at.
            long startedTicks = Stopwatch.GetTimestamp();

            try
            {
                steps[i].Run();
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Warn("CleanupService", $"Group '{steps[i].LabelKey}' failed.", ex);
            }

            AppLog.Debug("CleanupService", $"Group '{steps[i].LabelKey}' took {Stopwatch.GetElapsedTime(startedTicks).TotalMilliseconds:0} ms.");
        }

        return false;
    }

    /// <summary>
    /// Builds the ordered list of groups this run will process, skipping the ones the options or the
    /// running-state guards rule out. The list's length is what the progress bar scales against, so
    /// only steps that will actually execute belong in it.
    /// </summary>
    private List<(string LabelKey, Action Run)> BuildSteps(CleanupOptions options, CleanupTally tally, bool rustRunning)
    {
        List<(string LabelKey, Action Run)> steps =
        [
            ("ClearCacheStepTemp", () => ClearUserTemp(tally)),
            ("ClearCacheStepCrashDumps", () => ClearAppCrashDumps(tally)),
            ("ClearCacheStepLogs", () => ClearRustAndUnityLogs(tally))
        ];

        // Shader caches are only locked while the game itself is up, and deleting them mid-session
        // is the one way to actually break a running game rather than just slow its next launch.
        if (!rustRunning)
            steps.Add(("ClearCacheStepShaders", () => ClearShaderCaches(tally)));

        // No "is Steam running" guard: Steam is always running for the people this app is for, and
        // that guard made these targets dead code. This is only file deletion - if Steam genuinely
        // holds a file open, the delete fails and it's counted as skipped, same as anywhere else.
        steps.Add(("ClearCacheStepSteam", () => ClearSteamCaches(tally)));

        if (options.ClearThumbnailCache)
            steps.Add(("ClearCacheStepThumbnails", () => ClearThumbnailCache(tally)));

        if (options.EmptyRecycleBin)
            steps.Add(("ClearCacheStepRecycleBin", () => EmptyRecycleBin(tally)));

        return steps;
    }

    /// <summary>Clears the per-user temp directory, skipping anything written within <see cref="MinimumTempFileAge"/>.</summary>
    private static void ClearUserTemp(CleanupTally tally)
        => DeleteContents(Path.GetTempPath(), tally, MinimumTempFileAge);

    /// <summary>Clears per-application crash dumps. Unlike the kernel dumps, this location needs no admin rights.</summary>
    private static void ClearAppCrashDumps(CleanupTally tally)
        => DeleteContents(Path.Combine(LocalAppData, "CrashDumps"), tally);

    /// <summary>
    /// Clears every GPU shader cache that exists on this machine. Vendor detection isn't needed -
    /// a machine simply won't have the folders for GPUs it doesn't run, and hybrid laptops
    /// legitimately have two vendors' caches at once.
    /// </summary>
    private static void ClearShaderCaches(CleanupTally tally)
    {
        // Vendor-agnostic DirectX cache - present regardless of GPU.
        DeleteContents(Path.Combine(LocalAppData, "D3DSCache"), tally);

        // NVIDIA. ProgramData\NVIDIA Corporation\NV_Cache is deliberately absent: it was dropped in
        // driver 471.11 and folded into DXCache, and probing it on a modern driver only ever finds nothing.
        DeleteContents(Path.Combine(LocalAppData, "NVIDIA", "DXCache"), tally);
        DeleteContents(Path.Combine(LocalAppData, "NVIDIA", "GLCache"), tally);

        // AMD. Dx = DX11, Dxc = DX12, and post-2023 drivers reportedly consolidate into AMDCache.
        // All four are probed because the naming has drifted across driver generations.
        // TODO: Verify on gaming rig (AMD CPU + GPU)
        DeleteContents(Path.Combine(LocalAppData, "AMD", "DXCache"), tally);
        DeleteContents(Path.Combine(LocalAppData, "AMD", "DxcCache"), tally);
        DeleteContents(Path.Combine(LocalAppData, "AMD", "GLCache"), tally);
        DeleteContents(Path.Combine(LocalAppData, "AMD", "AMDCache"), tally);

        // Intel. LocalLow is the one confirmed on real hardware; the widely-cited LocalAppData
        // variant doesn't exist on a machine with a live Intel driver, but is probed in case newer
        // Arc drivers use it.
        DeleteContents(Path.Combine(LocalLowAppData, "Intel", "ShaderCache"), tally);
        DeleteContents(Path.Combine(LocalAppData, "Intel", "ShaderCache"), tally);
    }

    /// <summary>
    /// Clears Steam's download, depot and HTTP caches plus Rust's Steam-managed shader cache. Steam
    /// re-downloads anything still needed, so none of this is destructive - but the next launch
    /// recompiles shaders and will be slower.
    /// </summary>
    private void ClearSteamCaches(CleanupTally tally)
    {
        // TODO: Verify on gaming rig
        foreach (string library in _rustProcess.GetSteamLibraryFolders())
        {
            DeleteContents(Path.Combine(library, "steamapps", "downloading"), tally);
            DeleteContents(Path.Combine(library, "steamapps", "depotcache"), tally);
            DeleteContents(Path.Combine(library, "steamapps", "shadercache", SteamAppId), tally);
        }

        // appcache lives under the Steam install itself rather than each library folder.
        if (_rustProcess.GetSteamPath() is { } steamPath)
            DeleteContents(Path.Combine(steamPath, "appcache", "httpcache"), tally);
    }

    /// <summary>
    /// Deletes Rust's Unity player logs and Unity's own crash folders. Nothing rotates Player.log,
    /// so it grows for as long as the install lives.
    /// </summary>
    private static void ClearRustAndUnityLogs(CleanupTally tally)
    {
        // TODO: Verify on gaming rig
        string rustLogFolder = Path.Combine(LocalLowAppData, "Facepunch", "Rust");
        DeleteFileByPath(Path.Combine(rustLogFolder, "Player.log"), tally);
        DeleteFileByPath(Path.Combine(rustLogFolder, "Player-prev.log"), tally);

        DeleteContents(Path.Combine(LocalAppData, "Temp", "Facepunch Studios LTD"), tally);
    }

    /// <summary>
    /// Clears Explorer's thumbnail and icon caches.
    /// <para>
    /// Explorer is deliberately left running. It holds these files open - an exclusive open is
    /// refused - but it opens them with FILE_SHARE_DELETE, so deleting them outright works while the
    /// shell is live, which is why Disk Cleanup and other uninstallers don't restart it either.
    /// Killing and relaunching explorer.exe bought nothing and cost a black screen, a lost taskbar
    /// if the relaunch ever failed, and a child process that outlived the app.
    /// </para>
    /// </summary>
    private static void ClearThumbnailCache(CleanupTally tally)
    {
        string explorerCache = Path.Combine(LocalAppData, "Microsoft", "Windows", "Explorer");
        if (!Directory.Exists(explorerCache))
            return;

        foreach (string pattern in new[] { "thumbcache_*.db", "iconcache_*.db" })
            foreach (string file in SafeEnumerateFiles(explorerCache, pattern))
                DeleteFileByPath(file, tally);
    }

    /// <summary>
    /// Empties the Recycle Bin on every volume. The size is read first via <see cref="SHQueryRecycleBin"/>
    /// since the shell API reports nothing about what it deleted.
    /// </summary>
    private static void EmptyRecycleBin(CleanupTally tally)
    {
        try
        {
            SHQUERYRBINFO info = new() { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            long sizeBefore = SHQueryRecycleBin(null, ref info) == 0 ? info.i64Size : 0;

            const uint flags = SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND;
            if (SHEmptyRecycleBin(IntPtr.Zero, null, flags) == 0)
                tally.AddBytes(sizeBefore);
        }
        catch (Exception ex)
        {
            AppLog.Warn("CleanupService", "Failed to empty the Recycle Bin.", ex);
        }
    }

    /// <summary>
    /// Hands the admin-only targets to an elevated re-launch of this app - a single UAC prompt
    /// covering all of them - then folds the child's reported totals into <paramref name="tally"/>.
    /// Returns <see langword="false"/> only if the user declined the prompt.
    /// </summary>
    private static bool RunSystemTargetsElevated(CleanupTally tally)
    {
        // Cleared before launching so a leftover file from a previous crashed run can never be
        // mistaken for this run's result.
        DeleteElevationResultFile();

        ElevatedRunResult result = ElevationHelper.RunElevated(CleanupElevationRunner.Argument);
        if (result == ElevatedRunResult.CancelledByUser)
            return false;

        ReadElevationResult(tally);
        return true;
    }

    /// <summary>
    /// Reads the elevated run's totals from <see cref="CleanupElevationRunner.ResultFilePath"/> and
    /// deletes the file. A missing or malformed file just means nothing gets added - the deletions
    /// themselves already happened either way.
    /// </summary>
    private static void ReadElevationResult(CleanupTally tally)
    {
        try
        {
            if (!File.Exists(CleanupElevationRunner.ResultFilePath))
                return;

            string json = File.ReadAllText(CleanupElevationRunner.ResultFilePath);
            if (JsonSerializer.Deserialize<CleanupElevationResult>(json) is { } result)
                tally.Add(result.BytesFreed, result.FilesSkipped);
        }
        catch (Exception ex)
        {
            AppLog.Warn("CleanupService", "Failed to read the elevated cleanup result file.", ex);
        }
        finally
        {
            DeleteElevationResultFile();
        }
    }

    /// <summary>Removes the elevated run's result file, ignoring failures.</summary>
    private static void DeleteElevationResultFile()
    {
        try
        {
            if (File.Exists(CleanupElevationRunner.ResultFilePath))
                File.Delete(CleanupElevationRunner.ResultFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Warn("CleanupService", "Failed to delete the elevated cleanup result file.", ex);
        }
    }

    /// <summary>
    /// Deletes every file and subdirectory inside <paramref name="root"/>, leaving the root itself
    /// in place. Reparse points are never recursed into - a junction would otherwise redirect the
    /// delete somewhere entirely unintended. Files younger than <paramref name="minimumAge"/> are
    /// left alone when it's supplied.
    /// </summary>
    internal static void DeleteContents(string root, CleanupTally tally, TimeSpan? minimumAge = null)
    {
        if (!Directory.Exists(root))
        {
            // Absent targets are the normal case (no Steam, no AMD GPU, no Rust install), but
            // "which paths did it even look at" is the first question when a cleanup frees less
            // than someone expected.
            AppLog.Debug("CleanupService", $"Target '{root}' does not exist; skipped.");
            return;
        }

        DirectoryInfo rootInfo = new(root);
        if (rootInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            AppLog.Debug("CleanupService", $"Target '{root}' is a reparse point; not followed.");
            return;
        }

        long bytesBefore = tally.BytesFreed;

        // Parallelised only at the top level of each target. Most of these roots hold thousands of
        // small entries, and the cost is dominated by per-file syscalls rather than throughput, so
        // fanning out here is most of the win - recursing in parallel too would just oversubscribe.
        Parallel.ForEach(SafeEnumerateEntries(rootInfo), entry =>
        {
            if (entry is DirectoryInfo directory)
                DeleteDirectory(directory, tally, minimumAge);
            else if (entry is FileInfo file)
                DeleteFile(file, tally, minimumAge);
        });

        AppLog.Debug("CleanupService", $"Target '{root}' freed {tally.BytesFreed - bytesBefore} bytes.");
    }

    /// <summary>
    /// Recursively deletes <paramref name="directory"/>, returning whether it was fully removed.
    /// Handled entry-by-entry rather than via <see cref="Directory.Delete(string, bool)"/> so a
    /// single locked file doesn't abort the whole subtree, and so freed bytes can be tallied as
    /// they go.
    /// </summary>
    private static bool DeleteDirectory(DirectoryInfo directory, CleanupTally tally, TimeSpan? minimumAge)
    {
        // A junction or symlink is removed as a link, never followed - deleting its contents would
        // be deleting whatever it points at.
        if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            return DeleteDirectoryIfEmpty(directory, tally);

        bool emptied = true;

        foreach (FileSystemInfo entry in SafeEnumerateEntries(directory))
        {
            if (entry is DirectoryInfo child)
                emptied &= DeleteDirectory(child, tally, minimumAge);
            else if (entry is FileInfo file)
                emptied &= DeleteFile(file, tally, minimumAge);
        }

        // Attempted only when everything underneath actually went. Calling Delete() on a directory
        // that's still holding a locked file throws - and since a locked file is the normal case,
        // that turned an expected outcome into an exception per directory. Cheap to avoid: the
        // recursion above already knows whether it emptied.
        return emptied && DeleteDirectoryIfEmpty(directory, tally);
    }

    /// <summary>Deletes a single file by path, tallying its size. Missing files are not an error.</summary>
    internal static void DeleteFileByPath(string path, CleanupTally tally)
    {
        if (File.Exists(path))
            DeleteFile(new FileInfo(path), tally, null);
    }

    /// <summary>
    /// Deletes a single file, adding its size to <paramref name="tally"/>, and returns whether it's
    /// gone. A file still within <paramref name="minimumAge"/> is left alone and not counted as
    /// skipped - it was deliberately spared rather than failed on - but still reports false, since
    /// its directory can't be removed either.
    /// </summary>
    private static bool DeleteFile(FileInfo file, CleanupTally tally, TimeSpan? minimumAge)
    {
        try
        {
            if (minimumAge is { } age && DateTime.Now - file.LastWriteTime < age)
                return false;

            long size = file.Length;

            // Read-only files (common in extracted archives left in %TEMP%) refuse deletion outright.
            if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                file.Attributes &= ~FileAttributes.ReadOnly;

            file.Delete();
            tally.AddBytes(size);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Locked or protected files are the normal case during a cleanup, not a failure worth
            // surfacing - a running installer or game legitimately owns some of these.
            tally.AddSkipped();
            return false;
        }
        catch (Exception ex)
        {
            AppLog.Warn("CleanupService", $"Unexpected failure deleting '{file.FullName}'.", ex);
            tally.AddSkipped();
            return false;
        }
    }

    /// <summary>Deletes a now-empty directory by path, returning whether it's gone.</summary>
    internal static bool DeleteDirectoryIfEmpty(string path, CleanupTally tally)
        => !Directory.Exists(path) || DeleteDirectoryIfEmpty(new DirectoryInfo(path), tally);

    /// <summary>Deletes a now-empty directory, counting a failure as a skip rather than throwing.</summary>
    private static bool DeleteDirectoryIfEmpty(DirectoryInfo directory, CleanupTally tally)
    {
        try
        {
            directory.Delete();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            tally.AddSkipped();
            return false;
        }
    }

    /// <summary>
    /// Enumerates a directory's immediate children, returning nothing if the directory itself can't
    /// be read. Materialised into a list up front because the entries are about to be deleted, and
    /// lazily enumerating a directory while mutating it is undefined.
    /// </summary>
    private static IReadOnlyList<FileSystemInfo> SafeEnumerateEntries(DirectoryInfo directory)
    {
        try
        {
            return directory.GetFileSystemInfos();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Enumerates files matching a pattern, returning nothing if the directory can't be read.</summary>
    private static IReadOnlyList<string> SafeEnumerateFiles(string directory, string pattern)
    {
        try
        {
            return Directory.GetFiles(directory, pattern);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The current user's %LOCALAPPDATA% directory.</summary>
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// The current user's LocalLow directory. There's no <see cref="Environment.SpecialFolder"/> for
    /// it, so it's derived from %LOCALAPPDATA%'s sibling rather than resolved properly.
    /// </summary>
    private static string LocalLowAppData
        => Path.Combine(Path.GetDirectoryName(LocalAppData) ?? LocalAppData, "LocalLow");

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    /// <summary>Empties the Recycle Bin. A <paramref name="rootPath"/> of <see langword="null"/> means every volume.</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, uint flags);

    /// <summary>Reports the Recycle Bin's current size and item count, since <see cref="SHEmptyRecycleBin"/> reports neither.</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? rootPath, ref SHQUERYRBINFO info);

    /// <summary>Native layout for <see cref="SHQueryRecycleBin"/>'s result.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        /// <summary>Size of this struct in bytes; must be set before the call.</summary>
        public int cbSize;
        /// <summary>Total size of the Recycle Bin's contents, in bytes.</summary>
        public long i64Size;
        /// <summary>Number of items in the Recycle Bin.</summary>
        public long i64NumItems;
    }
}

/// <summary>
/// Running totals for a cleanup run. A mutable class rather than <c>ref</c> parameters so it can be
/// threaded through the recursive delete helpers without fighting the compiler.
/// </summary>
internal sealed class CleanupTally
{
    private long _bytesFreed;
    private int _filesSkipped;

    /// <summary>Total bytes reclaimed so far.</summary>
    public long BytesFreed => Interlocked.Read(ref _bytesFreed);

    /// <summary>How many files or directories couldn't be removed.</summary>
    public int FilesSkipped => Volatile.Read(ref _filesSkipped);

    /// <summary>Adds to the freed total. Safe to call from the parallel delete workers.</summary>
    public void AddBytes(long bytes) => Interlocked.Add(ref _bytesFreed, bytes);

    /// <summary>Records one entry that couldn't be deleted.</summary>
    public void AddSkipped() => Interlocked.Increment(ref _filesSkipped);

    /// <summary>Folds another run's totals in, used to merge the elevated child's reported results.</summary>
    public void Add(long bytes, int skipped)
    {
        Interlocked.Add(ref _bytesFreed, bytes);
        Interlocked.Add(ref _filesSkipped, skipped);
    }
}