using System.Threading.Tasks;
using System.Threading;
using System;

namespace RustOptimizer.Interface;

/// <summary>
/// One progress tick from a cleanup run, reported as each group of targets starts. Groups rather
/// than files: the per-file rate is far too uneven to make a meaningful bar, and a name the user
/// recognises ("Shader caches") is more informative than a file count they can't act on.
/// </summary>
/// <param name="LabelKey">Localization key naming the group being cleared, or "" once finished.</param>
/// <param name="CompletedSteps">How many groups have finished.</param>
/// <param name="TotalSteps">How many groups this run will process in total.</param>
public readonly record struct CleanupProgress(string LabelKey, int CompletedSteps, int TotalSteps);

/// <summary>
/// Which optional targets a cleanup run should include. Everything not represented here is
/// always cleared - those targets are fire-and-forget (worst case is a slower next game launch
/// while shaders recompile). Only the three genuinely consequential groups are toggleable, and
/// all three default to <see langword="true"/> in the UI, so these flags exist to let the user
/// opt <em>out</em> rather than in.
/// </summary>
/// <param name="EmptyRecycleBin">Whether to empty the Recycle Bin. Irreversible - files currently restorable are destroyed.</param>
/// <param name="ClearThumbnailCache">Whether to clear the thumbnail/icon cache, which requires killing and relaunching explorer.exe.</param>
/// <param name="IncludeSystemFiles">Whether to include the admin-only targets, which raises a UAC prompt for the run.</param>
public readonly record struct CleanupOptions(
    bool EmptyRecycleBin,
    bool ClearThumbnailCache,
    bool IncludeSystemFiles);

/// <summary>
/// What a cleanup run actually accomplished. Reported as a single status line under the Dashboard's
/// Clear Cache button rather than a per-target breakdown - the targets are an implementation
/// detail, the freed total is what the user asked for.
/// </summary>
/// <param name="BytesFreed">Total bytes reclaimed across every target that ran.</param>
/// <param name="FilesSkipped">How many files couldn't be deleted (locked, or access denied). Normal, not an error state.</param>
/// <param name="RustWasRunning">Whether Rust was running, causing every shader cache target to be skipped.</param>
/// <param name="ElevationDeclined">Whether the user declined the UAC prompt, causing the admin-only targets to be skipped.</param>
/// <param name="Cancelled">Whether the user stopped the run early, leaving later groups untouched.</param>
public readonly record struct CleanupOutcome(
    long BytesFreed,
    int FilesSkipped,
    bool RustWasRunning,
    bool ElevationDeclined,
    bool Cancelled);

/// <summary>
/// Deletes the caches, temp files, logs and crash dumps that accumulate on a gaming machine.
/// There is no separate scan pass - sizes are summed as files are deleted, since the UI reports
/// one total rather than a per-target checklist.
/// </summary>
public interface ICleanupService
{
    /// <summary>
    /// Runs a cleanup with the given <paramref name="options"/> off the UI thread. Never throws for
    /// a locked or access-denied file: those are counted in <see cref="CleanupOutcome.FilesSkipped"/>
    /// and the run continues. If <see cref="CleanupOptions.IncludeSystemFiles"/> is set, this
    /// re-launches the app elevated once (a single UAC prompt covering every admin-only target),
    /// started up front so it runs alongside the unelevated work rather than after it.
    /// <para>
    /// <paramref name="cancellationToken"/> stops the run between groups; the elevated child is a
    /// separate process and always runs to completion. A cancelled run still reports what it freed
    /// before stopping.
    /// </para>
    /// </summary>
    Task<CleanupOutcome> CleanAsync(CleanupOptions options, IProgress<CleanupProgress>? progress, CancellationToken cancellationToken);
}