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

/// <summary>
/// Reads real hardware identity strings and live usage figures for the Dashboard's System
/// Information card.
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
    /// Gets GPU 3D-engine usage as a percentage, or <see langword="null"/> if the underlying
    /// performance counters aren't available on this machine (e.g. some hybrid-graphics/driver
    /// combinations).
    /// </summary>
    double? GetGpuUsagePercent();

    /// <summary>
    /// Gets the RAM's current and rated (max) clock speed, as reported by the OS. Fields are <see langword="null"/> if unavailable.
    /// </summary>
    MemorySpeedInfo GetMemorySpeedInfo();
}