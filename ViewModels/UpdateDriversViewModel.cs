using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using RustOptimizer.Service;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the "Update Drivers" prompt. Checking whether a driver update is actually available would
/// need a version database per vendor this app doesn't have, so instead it shows what's detected for
/// the CPU, GPU and motherboard and links straight to whichever vendor's own driver page applies -
/// or, for a motherboard maker outside the handful covered, just names it so the user knows what to
/// search for. <see cref="CloseRequested"/> lets the hosting <c>UpdateDriversWindow</c> close itself
/// without this view model referencing <c>Window</c> directly.
/// </summary>
public sealed class UpdateDriversViewModel : ViewModelBase
{
    private readonly ISystemInfoService _systemInfo;
    private IReadOnlyList<DriverCheckRow> _rows = [];
    private bool _isLoading = true;

    public UpdateDriversViewModel(ILocalizationService localization, ISystemInfoService systemInfo) : base(localization)
    {
        _systemInfo = systemInfo;

        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
        OpenLinkCommand = new RelayCommand<string>(url =>
        {
            if (!string.IsNullOrEmpty(url))
                Utility.OpenUrl(url);
        });

        _ = LoadRowsAsync();
    }

    /// <summary>Raised when the user closes the prompt.</summary>
    public event Action? CloseRequested;

    /// <summary>One row per component checked: CPU, GPU and motherboard. Empty while <see cref="IsLoading"/> is true.</summary>
    public IReadOnlyList<DriverCheckRow> Rows
    {
        get => _rows;
        private set => SetProperty(ref _rows, value);
    }

    /// <summary>Whether the rows are still being resolved.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Opens the given URL, or does nothing if the row has no known vendor link.</summary>
    public RelayCommand<string> OpenLinkCommand { get; }

    /// <summary>Closes the prompt.</summary>
    public RelayCommand CloseCommand { get; }

    /// <summary>
    /// Resolves every row off the UI thread. Motherboard identity is a WMI query - CPU/GPU names stay
    /// a plain synchronous read since the Dashboard (the only way to reach this dialog) has already
    /// resolved and cached them by the time this opens.
    /// </summary>
    private async Task LoadRowsAsync()
    {
        string cpuName = _systemInfo.GetCpuName();
        string gpuName = _systemInfo.GetGpuName();
        MotherboardInfo motherboard = await Task.Run(_systemInfo.GetMotherboardInfo);

        Rows =
        [
            new DriverCheckRow(Localization["CpuLabel"], cpuName, DriverVendorLinks.ForCpu(cpuName)),
            new DriverCheckRow(Localization["GpuLabel"], gpuName, DriverVendorLinks.ForGpu(gpuName)),
            new DriverCheckRow(Localization["MotherboardLabel"], $"{motherboard.Manufacturer} {motherboard.Model}",
                DriverVendorLinks.ForMotherboard(motherboard.Manufacturer)),
            new DriverCheckRow(Localization["UpdateDriversOtherLabel"], Localization["UpdateDriversOtherDetected"], DriverVendorLinks.WindowsUpdate)
        ];
        IsLoading = false;
    }
}