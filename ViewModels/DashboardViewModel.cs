using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using Avalonia.Threading;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Dashboard's "System Information" card: real hardware identity strings (resolved once)
/// plus live CPU/GPU/RAM usage, refreshed every second while the Dashboard page is visible.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private const string NotAvailable = "N/A";

    private readonly ISystemInfoService _systemInfo;
    private DispatcherTimer? _pollTimer;

    private string _cpuName = "";
    private string _gpuName = "";
    private string _osDescription = "";
    private string _ramText = NotAvailable;
    private string _cpuUsageText = NotAvailable;
    private string _gpuUsageText = NotAvailable;

    public DashboardViewModel(ILocalizationService localization, ISystemInfoService systemInfo)
        : base(localization)
    {
        _systemInfo = systemInfo;

        RunSmartOptimizationCommand = new RelayCommand(() =>
        {
            // Mock data only for now - no real optimization logic wired up yet.
        });

        // Static identity strings never change, so these are resolved once here rather than on
        // every poll tick.
        CpuName = _systemInfo.GetCpuName();
        GpuName = _systemInfo.GetGpuName();
        OsDescription = _systemInfo.GetOsDescription();
        RamText = FormatMemory(_systemInfo.GetMemoryInfo());
    }

    public RelayCommand RunSmartOptimizationCommand { get; }

    public string CpuName { get => _cpuName; private set => SetProperty(ref _cpuName, value); }
    public string GpuName { get => _gpuName; private set => SetProperty(ref _gpuName, value); }
    public string OsDescription { get => _osDescription; private set => SetProperty(ref _osDescription, value); }
    public string RamText { get => _ramText; private set => SetProperty(ref _ramText, value); }
    public string CpuUsageText { get => _cpuUsageText; private set => SetProperty(ref _cpuUsageText, value); }
    public string GpuUsageText { get => _gpuUsageText; private set => SetProperty(ref _gpuUsageText, value); }

    /// <summary>
    /// Starts polling live usage every second. Call from the view's attach-to-visual-tree
    /// lifecycle so polling pauses while a different page is showing.
    /// </summary>
    public void StartPolling()
    {
        Poll();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();
    }

    /// <summary>
    /// Stops polling. Call from the view's detach-from-visual-tree lifecycle.
    /// </summary>
    public void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    private void Poll()
    {
        RamText = FormatMemory(_systemInfo.GetMemoryInfo());
        CpuUsageText = FormatPercent(_systemInfo.GetCpuUsagePercent());
        GpuUsageText = FormatPercent(_systemInfo.GetGpuUsagePercent());
    }

    private static string FormatMemory(MemoryInfo memory)
    {
        if (memory.TotalBytes == 0)
            return NotAvailable;

        const double bytesPerGb = 1024.0 * 1024.0 * 1024.0;
        double usedGb = memory.UsedBytes / bytesPerGb;
        double totalGb = memory.TotalBytes / bytesPerGb;
        double percent = memory.UsedBytes / (double)memory.TotalBytes * 100.0;

        return $"{usedGb:0.#} GB / {totalGb:0.#} GB ({percent:0}%)";
    }

    private static string FormatPercent(double? percent) => percent is { } value ? $"{value:0}%" : NotAvailable;
}