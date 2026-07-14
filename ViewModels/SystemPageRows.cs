using System.Collections.Generic;

namespace RustOptimizer.ViewModels;

/// <summary>One installed RAM stick, formatted for display in the System page's Memory card.</summary>
public sealed record RamModuleRow(string Slot, string CapacityText, string Manufacturer, string SpeedText);

/// <summary>
/// One physical storage device, formatted for display in the System page's Storage card, with the
/// drive letter(s) that live on it nested underneath - a disk with no drive letters (e.g. one
/// holding only an EFI/Recovery/MSR partition) just has an empty <see cref="Drives"/> list.
/// </summary>
public sealed record StorageDeviceRow(string Model, string MediaType, string CapacityText, IReadOnlyList<LogicalDriveRow> Drives);

/// <summary>One fixed logical drive, formatted for display (with a fill ratio for its usage bar) in the System page's Storage card.</summary>
public sealed record LogicalDriveRow(string Name, string UsageText, double PercentUsed);