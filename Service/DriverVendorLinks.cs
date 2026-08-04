using System;

namespace RustOptimizer.Service;

/// <summary>
/// Maps a detected CPU/GPU name or motherboard manufacturer string to that vendor's own driver/BIOS
/// download page. Deliberately doesn't try to detect whether an update is actually available - that
/// would need a version database per vendor this app doesn't have - it just gets the user to the
/// right place to check for themselves.
/// </summary>
internal static class DriverVendorLinks
{
    private const string NvidiaDrivers = "https://www.nvidia.com/en-us/geforce/drivers/";
    private const string AmdDrivers = "https://www.amd.com/en/support";
    private const string IntelDrivers = "https://www.intel.com/content/www/us/en/support/detect.html";
    private const string AsusSupport = "https://www.asus.com/support/";
    private const string MsiSupport = "https://www.msi.com/support/download";
    private const string GigabyteSupport = "https://www.gigabyte.com/Support";
    private const string AsRockSupport = "https://www.asrock.com/support/index.asp";

    /// <summary>
    /// Windows' own Windows Update page, which surfaces optional driver updates it already knows
    /// about - audio, network, chipset, USB controllers and anything else outside the CPU/GPU/motherboard
    /// checks above.
    /// </summary>
    public const string WindowsUpdate = "ms-settings:windowsupdate";

    /// <summary>The GPU vendor's driver page, or <see langword="null"/> if <paramref name="gpuName"/> doesn't name a recognized one.</summary>
    public static string? ForGpu(string gpuName) => gpuName switch
    {
        _ when Contains(gpuName, "NVIDIA") => NvidiaDrivers,
        _ when Contains(gpuName, "AMD") || Contains(gpuName, "Radeon") => AmdDrivers,
        _ when Contains(gpuName, "Intel") => IntelDrivers,
        _ => null
    };

    /// <summary>The CPU vendor's driver/chipset page, or <see langword="null"/> if <paramref name="cpuName"/> doesn't name a recognized one.</summary>
    public static string? ForCpu(string cpuName) => cpuName switch
    {
        _ when Contains(cpuName, "AMD") => AmdDrivers,
        _ when Contains(cpuName, "Intel") => IntelDrivers,
        _ => null
    };

    /// <summary>
    /// The motherboard maker's support page, or <see langword="null"/> for a manufacturer outside the
    /// handful of DIY-build brands covered here (an OEM prebuilt, or one this app doesn't recognize).
    /// </summary>
    public static string? ForMotherboard(string manufacturer) => manufacturer switch
    {
        _ when Contains(manufacturer, "ASUS") => AsusSupport,
        _ when Contains(manufacturer, "MSI") || Contains(manufacturer, "Micro-Star") => MsiSupport,
        _ when Contains(manufacturer, "Gigabyte") => GigabyteSupport,
        _ when Contains(manufacturer, "ASRock") => AsRockSupport,
        _ => null
    };

    private static bool Contains(string haystack, string needle) => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}