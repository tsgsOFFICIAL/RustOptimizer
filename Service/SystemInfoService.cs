using System.Runtime.InteropServices;
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
    // DEVMODE (wingdi.h): https://learn.microsoft.com/windows/win32/api/wingdi/ns-wingdi-devmodea
    // We only read dmPelsWidth/dmPelsHeight/dmDisplayFrequency, but EnumDisplaySettings writes into
    // this by byte offset, not by field name, so every member before them has to be declared too.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    // EnumDisplaySettings (winuser.h): https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-enumdisplaysettingsa
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    // -1 = ENUM_CURRENT_SETTINGS, per the docs above: gives the active mode instead of the next
    // entry in the supported-modes list.
    private const int EnumCurrentSettings = -1;

    private string? _cpuName;
    private string? _gpuName;
    private MemorySpeedInfo _memorySpeedInfo;
    private bool _memorySpeedResolved;
    private CpuDetails? _cpuDetails;
    private MotherboardInfo? _motherboardInfo;
    private BiosInfo? _biosInfo;
    private OsDetails? _osDetails;
    private string? _gpuDriverVersion;
    private bool _gpuDriverVersionResolved;

    private Computer? _hardwareMonitor;
    private IHardware? _cpuHardware;
    private IHardware? _gpuHardware;
    private IHardware? _memoryHardware;
    private ISensor? _cpuLoadSensor;
    private ISensor? _gpuLoadSensor;
    private ISensor? _gpuTempSensor;
    private ISensor? _gpuCoreClockSensor;
    private ISensor? _gpuMemoryClockSensor;
    private ISensor? _gpuFanSensor;
    private ISensor? _gpuPowerSensor;
    private ISensor? _gpuVramUsedSensor;
    private ISensor? _gpuVramTotalSensor;

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string GetOsDescription()
    {
        string bits = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        return $"{Utility.GetFriendlyOsName()} {bits}";
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public MemorySpeedInfo GetMemorySpeedInfo()
    {
        if (_memorySpeedResolved)
            return _memorySpeedInfo;

        _memorySpeedResolved = true;
        return _memorySpeedInfo = ReadMemorySpeedFromWmi();
    }

    /// <inheritdoc />
    public CpuDetails GetCpuDetails()
    {
        if (_cpuDetails is { } cached)
            return cached;

        CpuDetails details = default;
        try
        {
            using ManagementObjectSearcher searcher = new("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject processor in results)
            {
                using (processor)
                {
                    int? cores = processor["NumberOfCores"] is uint c ? (int)c : null;
                    int? logical = processor["NumberOfLogicalProcessors"] is uint l ? (int)l : null;
                    details = new CpuDetails(cores, logical);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read CPU core count via WMI.", ex);
        }

        _cpuDetails = details;
        return details;
    }

    /// <inheritdoc />
    public GpuSensorInfo GetGpuSensors()
    {
        try
        {
            EnsureHardwareMonitorOpen();
            _gpuHardware?.Update();

            return new GpuSensorInfo(
                LoadPercent: _gpuLoadSensor?.Value,
                TemperatureC: _gpuTempSensor?.Value,
                CoreClockMhz: _gpuCoreClockSensor?.Value,
                MemoryClockMhz: _gpuMemoryClockSensor?.Value,
                FanRpm: _gpuFanSensor?.Value,
                PowerWatts: _gpuPowerSensor?.Value,
                VramUsedMiB: _gpuVramUsedSensor?.Value,
                VramTotalMiB: _gpuVramTotalSensor?.Value);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read GPU sensors via LibreHardwareMonitor.", ex);
            return default;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RamModuleInfo> GetRamModules()
    {
        List<RamModuleInfo> modules = [];
        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT Manufacturer, PartNumber, DeviceLocator, Capacity, Speed FROM Win32_PhysicalMemory");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject module in results)
            {
                using (module)
                {
                    string manufacturer = (module["Manufacturer"] as string)?.Trim() ?? Unknown();
                    string partNumber = (module["PartNumber"] as string)?.Trim() ?? Unknown();
                    string slot = (module["DeviceLocator"] as string)?.Trim() ?? Unknown();
                    ulong capacity = module["Capacity"] is ulong cap ? cap : 0;
                    int? speed = module["Speed"] is uint s && s > 0 ? (int)s : null;

                    modules.Add(new RamModuleInfo(manufacturer, partNumber, slot, capacity, speed));
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read RAM modules via WMI.", ex);
        }

        return modules;
    }

    /// <inheritdoc />
    public MotherboardInfo GetMotherboardInfo()
    {
        if (_motherboardInfo is { } cached)
            return cached;

        MotherboardInfo info = new(Unknown(), Unknown());
        try
        {
            using ManagementObjectSearcher searcher = new("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject board in results)
            {
                using (board)
                {
                    string manufacturer = (board["Manufacturer"] as string)?.Trim() is { Length: > 0 } m ? m : Unknown();
                    string model = (board["Product"] as string)?.Trim() is { Length: > 0 } p ? p : Unknown();
                    info = new MotherboardInfo(manufacturer, model);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read motherboard info via WMI.", ex);
        }

        _motherboardInfo = info;
        return info;
    }

    /// <inheritdoc />
    public BiosInfo GetBiosInfo()
    {
        if (_biosInfo is { } cached)
            return cached;

        BiosInfo info = new(Unknown(), null);
        try
        {
            using ManagementObjectSearcher searcher = new("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject bios in results)
            {
                using (bios)
                {
                    string version = (bios["SMBIOSBIOSVersion"] as string)?.Trim() is { Length: > 0 } v ? v : Unknown();
                    DateTime? releaseDate = bios["ReleaseDate"] is string rd
                        ? ManagementDateTimeConverter.ToDateTime(rd)
                        : null;
                    info = new BiosInfo(version, releaseDate);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read BIOS info via WMI.", ex);
        }

        _biosInfo = info;
        return info;
    }

    /// <inheritdoc />
    public IReadOnlyList<StorageDeviceInfo> GetStorageDevices()
    {
        List<StorageDeviceInfo> devices = [];
        try
        {
            Dictionary<int, List<string>> driveLettersByDiskNumber = ReadDriveLettersByDiskNumber();
            Dictionary<string, LogicalDriveInfo> logicalDrivesByName = ReadLogicalDrivesByName();

            using ManagementObjectSearcher searcher = new(
                @"root\Microsoft\Windows\Storage", "SELECT DeviceId, FriendlyName, MediaType, Size FROM MSFT_PhysicalDisk");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject disk in results)
            {
                using (disk)
                {
                    string model = (disk["FriendlyName"] as string)?.Trim() is { Length: > 0 } n ? n : Unknown();
                    string mediaType = disk["MediaType"] is ushort mt ? DescribeMediaType(mt) : Unknown();
                    ulong capacity = disk["Size"] is ulong size ? size : 0;

                    List<LogicalDriveInfo> drives = [];
                    if (disk["DeviceId"] is string deviceId && int.TryParse(deviceId, out int diskNumber)
                        && driveLettersByDiskNumber.TryGetValue(diskNumber, out List<string>? letters))
                    {
                        foreach (string letter in letters)
                        {
                            if (logicalDrivesByName.TryGetValue(letter, out LogicalDriveInfo drive))
                                drives.Add(drive);
                        }
                    }

                    devices.Add(new StorageDeviceInfo(model, mediaType, capacity, drives));
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read storage devices via WMI.", ex);
        }

        return devices;
    }

    /// <inheritdoc />
    public OsDetails GetOsDetails()
    {
        if (_osDetails is { } cached)
            return cached;

        OsDetails details = default;
        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT BuildNumber, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject os in results)
            {
                using (os)
                {
                    int? buildNumber = os["BuildNumber"] is string b && int.TryParse(b, out int parsed) ? parsed : null;
                    DateTime? installDate = os["InstallDate"] is string id ? ManagementDateTimeConverter.ToDateTime(id) : null;
                    DateTime? lastBoot = os["LastBootUpTime"] is string lb ? ManagementDateTimeConverter.ToDateTime(lb) : null;
                    details = new OsDetails(buildNumber, installDate, lastBoot);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read OS details via WMI.", ex);
        }

        _osDetails = details;
        return details;
    }

    /// <inheritdoc />
    public string? GetGpuDriverVersion()
    {
        if (_gpuDriverVersionResolved)
            return _gpuDriverVersion;

        _gpuDriverVersionResolved = true;
        try
        {
            string gpuName = GetGpuName();
            using ManagementObjectSearcher searcher = new("SELECT Name, DriverVersion FROM Win32_VideoController");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject controller in results)
            {
                using (controller)
                {
                    if (controller["Name"] is string name &&
                        (gpuName.Contains(name, StringComparison.OrdinalIgnoreCase) || name.Contains(gpuName, StringComparison.OrdinalIgnoreCase)))
                        return _gpuDriverVersion = controller["DriverVersion"] as string;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read GPU driver version via WMI.", ex);
        }

        return _gpuDriverVersion;
    }

    /// <inheritdoc />
    public DisplayModeInfo GetDisplayModeInfo()
    {
        try
        {
            DEVMODE current = default;
            current.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            if (!EnumDisplaySettings(null, EnumCurrentSettings, ref current) || current.dmPelsWidth == 0)
                return default;

            // Max resolution/Hz aren't reported anywhere directly - only found by walking every mode
            // the driver lists. Hz search stays scoped to the current resolution: a lower res can
            // unlock a higher Hz that has nothing to do with what's actually on screen.
            int maxWidth = current.dmPelsWidth;
            int maxHeight = current.dmPelsHeight;
            int maxHz = current.dmDisplayFrequency;

            for (int i = 0; ; i++)
            {
                DEVMODE mode = default;
                mode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                if (!EnumDisplaySettings(null, i, ref mode))
                    break;

                if ((long)mode.dmPelsWidth * mode.dmPelsHeight > (long)maxWidth * maxHeight)
                    (maxWidth, maxHeight) = (mode.dmPelsWidth, mode.dmPelsHeight);

                if (mode.dmPelsWidth == current.dmPelsWidth && mode.dmPelsHeight == current.dmPelsHeight
                    && mode.dmDisplayFrequency > maxHz)
                    maxHz = mode.dmDisplayFrequency;
            }

            // 0 or 1 means "use the display's default rate" per the docs above, not a real Hz value.
            int? currentHz = current.dmDisplayFrequency > 1 ? current.dmDisplayFrequency : null;
            return new DisplayModeInfo(current.dmPelsWidth, current.dmPelsHeight, currentHz, maxWidth, maxHeight, maxHz > 1 ? maxHz : null);
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read display mode via EnumDisplaySettings.", ex);
            return default;
        }
    }

    /// <summary>Maps a Storage Management API <c>MediaType</c> code to "SSD"/"HDD"/"SCM".</summary>
    private static string DescribeMediaType(ushort mediaType) => mediaType switch
    {
        3 => "HDD",
        4 => "SSD",
        5 => "SCM",
        _ => "Unknown"
    };

    /// <summary>
    /// Maps each physical disk's number to the drive letter(s) (e.g. "C:") that live on it, via
    /// <c>MSFT_Partition</c>. A disk holding only letterless partitions (EFI, Recovery, MSR) simply
    /// won't appear as a key here.
    /// </summary>
    private static Dictionary<int, List<string>> ReadDriveLettersByDiskNumber()
    {
        Dictionary<int, List<string>> result = [];
        try
        {
            using ManagementObjectSearcher searcher = new(
                @"root\Microsoft\Windows\Storage", "SELECT DiskNumber, DriveLetter FROM MSFT_Partition");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject partition in results)
            {
                using (partition)
                {
                    if (partition["DiskNumber"] is not uint diskNumber)
                        continue;

                    // DriveLetter is char16 in the Storage Management API; System.Management can
                    // surface that as a System.Char or a single-character string. '\0' means
                    // "no letter" either way (EFI/Recovery/MSR partitions).
                    char? letter = partition["DriveLetter"] switch
                    {
                        char c when c != '\0' => c,
                        string s when s.Length == 1 && s[0] != '\0' => s[0],
                        _ => null
                    };

                    if (letter is not { } value)
                        continue;

                    if (!result.TryGetValue((int)diskNumber, out List<string>? letters))
                        result[(int)diskNumber] = letters = [];

                    letters.Add($"{value}:");
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read partition-to-disk mapping via WMI.", ex);
        }

        return result;
    }

    /// <summary>Reads every fixed logical drive (drive letter, total/free space), keyed by drive letter (e.g. "C:").</summary>
    private static Dictionary<string, LogicalDriveInfo> ReadLogicalDrivesByName()
    {
        Dictionary<string, LogicalDriveInfo> result = [];
        try
        {
            // DriveType=3 is "Local Disk" (fixed) - excludes removable media, network shares and
            // optical drives.
            using ManagementObjectSearcher searcher = new(
                "SELECT DeviceID, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject drive in results)
            {
                using (drive)
                {
                    if (drive["DeviceID"] is not string name)
                        continue;

                    ulong total = drive["Size"] is ulong size ? size : 0;
                    ulong free = drive["FreeSpace"] is ulong freeSpace ? freeSpace : 0;

                    result[name] = new LogicalDriveInfo(name, total, free);
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("SystemInfoService", "Failed to read logical drives via WMI.", ex);
        }

        return result;
    }

    /// <summary>Reads current (<c>ConfiguredClockSpeed</c>) and rated (<c>Speed</c>) DRAM speed via WMI.</summary>
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
    /// Opens LibreHardwareMonitor once and resolves the CPU/GPU/memory hardware handles and their
    /// sensors. Load sensors work without administrator rights; temperature/clock/voltage sensors
    /// need the kernel driver and come back null unelevated.
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

        // Prefer a discrete adapter's own load sensor ("GPU Core") over an integrated one, which
        // only exposes per-engine D3D counters - matches the GetGpuName() preference.
        _gpuHardware = SelectPrimaryGpu(_hardwareMonitor.Hardware.Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd))
            ?? SelectPrimaryGpu(_hardwareMonitor.Hardware.Where(h => h.HardwareType == HardwareType.GpuIntel));

        _gpuLoadSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "D3D 3D")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);

        // Not every adapter exposes every sensor (e.g. laptop GPUs commonly report no fan), so
        // these can legitimately end up null.
        _gpuTempSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "GPU Core")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
        _gpuCoreClockSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && s.Name == "GPU Core");
        _gpuMemoryClockSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && s.Name == "GPU Memory");
        _gpuFanSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan);
        _gpuPowerSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && s.Name == "GPU Package")
            ?? _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
        _gpuVramUsedSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Used");
        _gpuVramTotalSensor = _gpuHardware?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Total");

        // LibreHardwareMonitor exposes physical RAM and the page file as two separate "Memory"
        // entries ("Total Memory" and "Virtual Memory") - we only want the former.
        _memoryHardware = _hardwareMonitor.Hardware
            .FirstOrDefault(h => h.HardwareType == HardwareType.Memory && h.Name == "Total Memory");
    }

    /// <summary>
    /// Picks the adapter with the most VRAM out of a same-type group, since a discrete GPU and its
    /// host's integrated GPU can report the same <see cref="HardwareType"/>.
    /// </summary>
    private static IHardware? SelectPrimaryGpu(IEnumerable<IHardware> candidates)
    {
        IHardware[] gpus = candidates.ToArray();
        return gpus.Length <= 1 ? gpus.FirstOrDefault() : gpus.MaxBy(GetTotalMemoryMiB);
    }

    /// <summary>Reads a GPU hardware handle's total VRAM, in MiB.</summary>
    private static double GetTotalMemoryMiB(IHardware hardware)
    {
        hardware.Update();
        return hardware.Sensors
            .Where(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Total")
            .Select(s => s.Value ?? 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    /// <summary>The localized fallback string used whenever hardware detection fails.</summary>
    private string Unknown() => localization["UnknownHardware"];

    /// <summary>Collapses repeated whitespace in a hardware name string.</summary>
    private static string Normalize(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Refreshes every hardware/sub-hardware sensor - LibreHardwareMonitor has no single
    /// "update everything" call on <see cref="Computer"/> itself.
    /// </summary>
    private sealed class SensorUpdateVisitor : IVisitor
    {
        /// <summary>Traverses the whole hardware tree.</summary>
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        /// <summary>Updates one hardware node and recurses into its sub-hardware.</summary>
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware sub in hardware.SubHardware)
                sub.Accept(this);
        }

        /// <summary>No-op; sensors are refreshed as part of their owning hardware's <see cref="VisitHardware"/>.</summary>
        public void VisitSensor(ISensor sensor)
        {
        }

        /// <summary>No-op; parameters aren't used by this app.</summary>
        public void VisitParameter(IParameter parameter)
        {
        }
    }
}