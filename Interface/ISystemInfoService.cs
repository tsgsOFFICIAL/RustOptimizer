using System.Collections.Generic;
using System;

namespace RustOptimizer.Interface;

/// <summary>
/// Total and used physical memory, in bytes.
/// </summary>
public readonly record struct MemoryInfo(ulong TotalBytes, ulong UsedBytes);

/// <summary>
/// RAM clock speed in MHz: <see cref="CurrentMhz"/> is what it's actually running at right now,
/// <see cref="RatedMhz"/> is the module's maximum rated speed (e.g. its XMP/EXPO profile). They
/// differ when the rated profile isn't enabled in BIOS, in which case RAM runs at a slower
/// JEDEC default speed.
/// </summary>
public readonly record struct MemorySpeedInfo(int? CurrentMhz, int? RatedMhz);

/// <summary>Core and logical processor counts for the CPU.</summary>
public readonly record struct CpuDetails(int? CoreCount, int? LogicalProcessorCount);

/// <summary>
/// Live GPU sensor readings, including 3D-engine load. Fields are individually <see langword="null"/>
/// when that sensor isn't exposed by the adapter/vendor (e.g. many laptop GPUs report no fan
/// sensor) - omit rather than show a blank/zero value for a null field. One call for everything,
/// not one method per sensor, since each read needs a hardware-handle update first (~60ms on this
/// GPU/driver) that splitting into separate calls would multiply.
/// </summary>
public readonly record struct GpuSensorInfo(
    double? LoadPercent,
    double? TemperatureC,
    double? CoreClockMhz,
    double? MemoryClockMhz,
    double? FanRpm,
    double? PowerWatts,
    double? VramUsedMiB,
    double? VramTotalMiB);

/// <summary>One installed RAM stick, as reported by <c>Win32_PhysicalMemory</c>.</summary>
public readonly record struct RamModuleInfo(string Manufacturer, string PartNumber, string Slot, ulong CapacityBytes, int? SpeedMhz);

/// <summary>Motherboard identity, as reported by <c>Win32_BaseBoard</c>.</summary>
public readonly record struct MotherboardInfo(string Manufacturer, string Model);

/// <summary>BIOS/firmware version and release date, as reported by <c>Win32_BIOS</c>.</summary>
public readonly record struct BiosInfo(string Version, DateTime? ReleaseDate);

/// <summary>One fixed logical drive (e.g. "C:"), as reported by <c>Win32_LogicalDisk</c>.</summary>
public readonly record struct LogicalDriveInfo(string Name, ulong TotalBytes, ulong FreeBytes);

/// <summary>
/// One physical storage device, as reported by <c>MSFT_PhysicalDisk</c>, with the drive letter(s)
/// that live on it - correlated via <c>MSFT_Partition.DiskNumber</c>/<c>DriveLetter</c>. A disk can
/// have zero drive letters here (e.g. one holding only an EFI/Recovery/MSR partition).
/// </summary>
public readonly record struct StorageDeviceInfo(string Model, string MediaType, ulong CapacityBytes, IReadOnlyList<LogicalDriveInfo> Drives);

/// <summary>Build/install/uptime details, as reported by <c>Win32_OperatingSystem</c>.</summary>
public readonly record struct OsDetails(int? BuildNumber, DateTime? InstallDate, DateTime? LastBootUpTime);

/// <summary>
/// The primary monitor's current resolution/refresh rate versus the highest it supports - e.g. to
/// catch a 144Hz-capable panel left running at 60Hz, or a 4K panel running at 1080p. All fields are
/// <see langword="null"/> together if the display driver couldn't be queried.
/// </summary>
public readonly record struct DisplayModeInfo(
    int? CurrentWidth,
    int? CurrentHeight,
    int? CurrentHz,
    int? MaxWidth,
    int? MaxHeight,
    int? MaxHz);

/// <summary>
/// Reads real hardware identity strings and live usage figures for the Dashboard's System
/// Information card and the full System page.
/// </summary>
public interface ISystemInfoService
{
    /// <summary>Gets the CPU model name (e.g. "AMD Ryzen 5 5600X"), or a fallback if detection fails.</summary>
    string GetCpuName();

    /// <summary>Gets the primary GPU model name (e.g. "NVIDIA GeForce RTX 3060"), or a fallback if detection fails.</summary>
    string GetGpuName();

    /// <summary>Gets a human-readable OS description (e.g. "Windows 11 64-bit").</summary>
    string GetOsDescription();

    /// <summary>Gets current total/used physical memory. Instant - no sampling delay.</summary>
    MemoryInfo GetMemoryInfo();

    /// <summary>
    /// Gets system-wide CPU load as a percentage, measured since the previous call.
    /// Returns <see langword="null"/> on the first call, since there's no prior sample yet.
    /// </summary>
    double? GetCpuUsagePercent();

    /// <summary>
    /// Gets the RAM's current and rated (max) clock speed, as reported by the OS. Fields are <see langword="null"/> if unavailable.
    /// </summary>
    MemorySpeedInfo GetMemorySpeedInfo();

    /// <summary>Gets the CPU's core/logical processor counts. Resolved once and cached.</summary>
    CpuDetails GetCpuDetails();

    /// <summary>
    /// Gets live GPU sensor readings (load, temperature, clocks, fan, power draw, VRAM usage) for
    /// the primary GPU in a single hardware update. Confirmed to read without administrator rights
    /// on NVIDIA/Intel hardware; individual fields come back <see langword="null"/> on
    /// adapters/vendors that don't expose that particular sensor.
    /// </summary>
    GpuSensorInfo GetGpuSensors();

    /// <summary>Gets per-slot RAM module details (manufacturer, part number, slot, capacity, speed).</summary>
    IReadOnlyList<RamModuleInfo> GetRamModules();

    /// <summary>Gets the motherboard's manufacturer/model. Resolved once and cached.</summary>
    MotherboardInfo GetMotherboardInfo();

    /// <summary>Gets the BIOS/firmware version and release date. Resolved once and cached.</summary>
    BiosInfo GetBiosInfo();

    /// <summary>Gets every physical storage device (model, SSD/HDD, capacity), each with its drive letter(s) nested underneath.</summary>
    IReadOnlyList<StorageDeviceInfo> GetStorageDevices();

    /// <summary>Gets OS build number, install date and last boot time. Resolved once and cached.</summary>
    OsDetails GetOsDetails();

    /// <summary>Gets the primary GPU's driver version (e.g. "32.0.15.8160"), or <see langword="null"/> if unavailable.</summary>
    string? GetGpuDriverVersion();

    /// <summary>Gets the primary monitor's current resolution/refresh rate and the highest it supports.</summary>
    DisplayModeInfo GetDisplayModeInfo();
}