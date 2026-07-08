using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using RustOptimizer.Service.Logging;
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

    private Computer? _hardwareMonitor;
    private IHardware? _cpuHardware;
    private IHardware? _gpuHardware;
    private ISensor? _cpuLoadSensor;
    private ISensor? _gpuLoadSensor;

    public string GetCpuName()
    {
        if (_cpuName != null)
            return _cpuName;

        try
        {
            using ManagementObjectSearcher searcher = new("SELECT Name FROM Win32_Processor");
            foreach (ManagementBaseObject obj in searcher.Get())
            {
                if (obj["Name"] is string name && !string.IsNullOrWhiteSpace(name))
                    return _cpuName = Normalize(name);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to query CPU name via WMI.", ex);
        }

        return _cpuName = Unknown();
    }

    public string GetGpuName()
    {
        if (_gpuName != null)
            return _gpuName;

        try
        {
            using ManagementObjectSearcher searcher = new("SELECT Name FROM Win32_VideoController");
            string? fallback = null;

            foreach (ManagementBaseObject obj in searcher.Get())
            {
                if (obj["Name"] is not string name || string.IsNullOrWhiteSpace(name))
                    continue;

                // Prefer a real adapter over the software rasterizer Windows always lists alongside it.
                if (!name.Contains("Basic Render", StringComparison.OrdinalIgnoreCase))
                    return _gpuName = Normalize(name);

                fallback ??= Normalize(name);
            }

            if (fallback != null)
                return _gpuName = fallback;
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to query GPU name via WMI.", ex);
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
        MEMORYSTATUSEX status = new() { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            return default;

        return new MemoryInfo(status.ullTotalPhys, status.ullTotalPhys - status.ullAvailPhys);
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

        _hardwareMonitor = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
        _hardwareMonitor.Open();
        _hardwareMonitor.Accept(new SensorUpdateVisitor());

        _cpuHardware = _hardwareMonitor.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        _cpuLoadSensor = _cpuHardware?.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");

        // Prefer a discrete adapter's own load sensor ("GPU Core") over an integrated one,
        // which only exposes per-engine D3D counters instead - matches the GetGpuName() preference.
        _gpuHardware = _hardwareMonitor.Hardware
            .FirstOrDefault(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd)
            ?? _hardwareMonitor.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel);

        _gpuLoadSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "D3D 3D")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
    }

    private string Unknown() => localization["UnknownHardware"];

    private static string Normalize(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

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