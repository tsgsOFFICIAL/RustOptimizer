using RustOptimizer.Interface;
using System.Diagnostics;

namespace RustOptimizer.Service;

/// <inheritdoc cref="IRustProcessService" />
public sealed class RustProcessService : IRustProcessService
{
    private const string ProcessName = "RustClient";
    private const string SteamAppId = "252490";

    public bool IsRunning()
    {
        Process[] processes = Process.GetProcessesByName(ProcessName);
        foreach (Process process in processes)
            process.Dispose();

        return processes.Length > 0;
    }

    public void Launch() => Utility.OpenUrl($"steam://rungameid/{SteamAppId}");
    
    public void VerifyFiles() => Utility.OpenUrl($"steam://validate/{SteamAppId}");
}
