using LibreHardwareMonitor.Hardware;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.Interface;
using System.Management;
using System.Linq;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="ISystemInfoService" />
[SupportedOSPlatform("windows")]
public sealed class SystemInfoService(ILocalizationService localization) : ISystemInfoService
{
    private string? _cpuName;
    private string? _gpuName;
    private MemorySpeedInfo _memorySpeedInfo;
    private bool _memorySpeedResolved;

    private Computer? _hardwareMonitor;
    private IHardware? _cpuHardware;
    private IHardware? _gpuHardware;
    private IHardware? _memoryHardware;
    private ISensor? _cpuLoadSensor;
    private ISensor? _gpuLoadSensor;

    public string GetCpuName()
    {
        if (_cpuName != null)
            return _cpuName;

        try
        {
            EnsureHardwareMonitorOpen();
            if (_cpuHardware?.Name is string name && !string.IsNullOrWhiteSpace(name))
                return _cpuName = Normalize(name);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to query CPU name via LibreHardwareMonitor.", ex);
        }

        return _cpuName = Unknown();
    }

    public string GetGpuName()
    {
        if (_gpuName != null)
            return _gpuName;

        try
        {
            EnsureHardwareMonitorOpen();
            if (_gpuHardware?.Name is string name && !string.IsNullOrWhiteSpace(name))
                return _gpuName = Normalize(name);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to query GPU name via LibreHardwareMonitor.", ex);
        }

        return _gpuName = Unknown();
    }

    public string GetOsDescription()
    {
        string bits = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        return $"{Utility.GetFriendlyOsName()} {bits}";
    }

    public MemoryInfo GetMemoryInfo()
    {
        try
        {
            EnsureHardwareMonitorOpen();
            _memoryHardware?.Update();

            double? usedGb = _memoryHardware?.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used")?.Value;
            double? availableGb = _memoryHardware?.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available")?.Value;

            if (usedGb is not { } used || availableGb is not { } available)
                return default;

            const double bytesPerGb = 1024 * 1024 * 1024;
            ulong usedBytes = (ulong)(used * bytesPerGb);
            ulong totalBytes = (ulong)((used + available) * bytesPerGb);
            return new MemoryInfo(totalBytes, usedBytes);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read memory info via LibreHardwareMonitor.", ex);
            return default;
        }
    }

    public double? GetCpuUsagePercent()
    {
        try
        {
            EnsureHardwareMonitorOpen();
            _cpuHardware?.Update();
            return _cpuLoadSensor?.Value is { } value ? value : null;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read CPU load via LibreHardwareMonitor.", ex);
            return null;
        }
    }

    public double? GetGpuUsagePercent()
    {
        try
        {
            EnsureHardwareMonitorOpen();
            _gpuHardware?.Update();
            return _gpuLoadSensor?.Value is { } value ? value : null;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read GPU load via LibreHardwareMonitor.", ex);
            return null;
        }
    }

    public MemorySpeedInfo GetMemorySpeedInfo()
    {
        if (_memorySpeedResolved)
            return _memorySpeedInfo;

        _memorySpeedResolved = true;
        return _memorySpeedInfo = ReadMemorySpeedFromWmi();
    }

    /// <summary>
    /// Reads current (<c>ConfiguredClockSpeed</c>) and maximum rated (<c>Speed</c>) DRAM speed via
    /// WMI - the same source Task Manager's Performance tab uses.
    /// </summary>
    private static MemorySpeedInfo ReadMemorySpeedFromWmi()
    {
        try
        {
            using ManagementObjectSearcher searcher = new("SELECT ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory");
            using ManagementObjectCollection modules = searcher.Get();

            foreach (ManagementBaseObject module in modules)
            {
                using (module)
                {
                    int? current = module["ConfiguredClockSpeed"] is uint c && c > 0 ? (int)c : null;
                    int? rated = module["Speed"] is uint r && r > 0 ? (int)r : null;

                    if (current != null || rated != null)
                        return new MemorySpeedInfo(current, rated);
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read memory speed via WMI.", ex);
        }

        return default;
    }

    /// <summary>
    /// Opens LibreHardwareMonitor and picks out the one CPU load sensor and one GPU load sensor we
    /// care about, once. Chosen this way (rather than raw P/Invoke deltas or "GPU Engine" performance
    /// counters, both tried first) because it reads CPU/GPU load correctly without administrator
    /// rights - confirmed by testing unelevated - and because it separates GPU load per physical
    /// adapter, which per-process "GPU Engine" counters don't (they blend every adapter together).
    /// Sensors needing the kernel driver (temperature, clock, voltage) come back null without
    /// elevation, but load doesn't depend on those.
    /// </summary>
    private void EnsureHardwareMonitorOpen()
    {
        if (_hardwareMonitor != null)
            return;

        _hardwareMonitor = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true };
        _hardwareMonitor.Open();
        _hardwareMonitor.Accept(new SensorUpdateVisitor());

        _cpuHardware = _hardwareMonitor.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        _cpuLoadSensor = _cpuHardware?.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");

        // Prefer a discrete adapter's own load sensor ("GPU Core") over an integrated one,
        // which only exposes per-engine D3D counters instead - matches the GetGpuName() preference.
        _gpuHardware = SelectPrimaryGpu(_hardwareMonitor.Hardware.Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd))
            ?? SelectPrimaryGpu(_hardwareMonitor.Hardware.Where(h => h.HardwareType == HardwareType.GpuIntel));

        _gpuLoadSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "D3D 3D")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);

        // LibreHardwareMonitor exposes physical RAM and the page file as two separate "Memory"
        // hardware entries ("Total Memory" and "Virtual Memory") - we only want the former.
        _memoryHardware = _hardwareMonitor.Hardware
            .FirstOrDefault(h => h.HardwareType == HardwareType.Memory && h.Name == "Total Memory");
    }

    /// <summary>
    /// Picks the adapter with the most total VRAM out of a same-type group. A discrete GPU
    /// alongside its host's integrated GPU (e.g. an AMD APU's Radeon graphics next to a Radeon
    /// RX card) both report the same <see cref="HardwareType"/>, so type alone can't tell them
    /// apart - total video memory does, since a discrete card reports several GB while an
    /// APU's shared allocation is a small fraction of that.
    /// </summary>
    private static IHardware? SelectPrimaryGpu(IEnumerable<IHardware> candidates)
    {
        IHardware[] gpus = candidates.ToArray();
        return gpus.Length <= 1 ? gpus.FirstOrDefault() : gpus.MaxBy(GetTotalMemoryMiB);
    }

    private static double GetTotalMemoryMiB(IHardware hardware)
    {
        hardware.Update();
        return hardware.Sensors
            .Where(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Total")
            .Select(s => s.Value ?? 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private string Unknown() => localization["UnknownHardware"];

    private static string Normalize(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// LibreHardwareMonitor requires visiting hardware/sub-hardware explicitly to refresh sensors -
    /// there's no simple "update everything" call on <see cref="Computer"/> itself.
    /// </summary>
    private sealed class SensorUpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware sub in hardware.SubHardware)
                sub.Accept(this);
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}