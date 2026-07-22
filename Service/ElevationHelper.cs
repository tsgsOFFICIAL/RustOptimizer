using RustOptimizer.Service.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System;

namespace RustOptimizer.Service;

/// <summary>The outcome of an elevated re-launch via <see cref="ElevationHelper.RunElevated"/>.</summary>
public enum ElevatedRunResult
{
    /// <summary>The elevated process ran and reported success (exit code 0).</summary>
    Success,
    /// <summary>The user declined the UAC prompt - nothing was attempted.</summary>
    CancelledByUser,
    /// <summary>The elevated process couldn't be started, or it ran and reported failure.</summary>
    Failed
}

/// <summary>
/// Re-launches this app's own executable elevated to perform a single privileged action, then
/// waits for it to exit. This is the app's only elevation mechanism - everything else runs
/// unelevated. Callers pass the exact command-line arguments the elevated re-launch should
/// receive (see <see cref="NetworkTweakElevationRunner"/> for the one consumer today).
/// </summary>
public static class ElevationHelper
{
    /// <summary>
    /// Re-launches <see cref="Utility.GetExePath"/> elevated (triggering a UAC prompt) with the
    /// given arguments, and waits for it to exit. <see cref="ProcessStartInfo.UseShellExecute"/>
    /// must be <see langword="true"/> for <c>Verb="runas"</c> to work, which also means the
    /// elevated process's stdout/stderr can't be captured here - it logs via <see cref="AppLog"/>
    /// instead, to the same file, since <c>runas</c> elevates the same user account.
    /// </summary>
    public static ElevatedRunResult RunElevated(params string[] args)
    {
        try
        {
            ProcessStartInfo startInfo = new(Utility.GetExePath()) { UseShellExecute = true, Verb = "runas" };
            foreach (string arg in args)
                startInfo.ArgumentList.Add(arg);

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                AppLog.Warn("ElevationHelper", "Process.Start returned null for the elevated re-launch.");
                return ElevatedRunResult.Failed;
            }

            process.WaitForExit();
            return process.ExitCode == 0 ? ElevatedRunResult.Success : ElevatedRunResult.Failed;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED - the user declined the UAC prompt.
        {
            return ElevatedRunResult.CancelledByUser;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ElevationHelper", "Failed to run the elevated helper process.", ex);
            return ElevatedRunResult.Failed;
        }
    }
}