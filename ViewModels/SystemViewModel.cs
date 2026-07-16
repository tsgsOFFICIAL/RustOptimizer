using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the System page: hardware/OS identity and live usage data, plus per-module RAM info,
/// motherboard/BIOS, storage devices, GPU sensors, and a handful of OS-level tweaks.
/// </summary>
public sealed class SystemViewModel : ViewModelBase
{
    private const string NotAvailable = "N/A";

    // Distinct from NotAvailable: a field can sit here for several seconds (GPU driver version in
    // particular - Win32_VideoController is a slow WMI query on some machines) and "N/A" would
    // misleadingly read as "unavailable" rather than "still working."
    private const string Loading = "…";

    private readonly ISystemInfoService _systemInfo;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly IRustProcessService _rustProcess;
    private DispatcherTimer? _pollTimer;
    private bool _isPolling;
    private int _storagePollTickCount;

    // Free space changes far more slowly than CPU/GPU/RAM usage, and re-enumerating physical
    // disks via WMI (MSFT_PhysicalDisk plus the disk-to-partition joins) is comparatively
    // expensive, so storage only refreshes every Nth tick of the 1-second poll timer rather than
    // every tick.
    private const int StoragePollEveryNTicks = 5;

    private string _cpuName = "";
    private string _cpuCoresText = Loading;
    private string _gpuName = "";
    private string _gpuDriverVersionText = Loading;
    private string _osDescription = "";
    private string _osBuildText = Loading;
    private string _osInstallDateText = Loading;
    private string _osUptimeText = NotAvailable;
    private string _ramText = NotAvailable;
    private string _memorySpeedText = Loading;
    private string _maxMemorySpeedText = Loading;
    private string _cpuUsageText = NotAvailable;
    private string _gpuUsageText = NotAvailable;
    private double _cpuUsagePercent;
    private double _gpuUsagePercent;
    private double _ramUsagePercent;
    private string _motherboardText = Loading;
    private string _biosText = Loading;

    private string _gpuTemperatureText = NotAvailable;
    private string _gpuCoreClockText = NotAvailable;
    private string _gpuMemoryClockText = NotAvailable;
    private string _gpuFanText = NotAvailable;
    private string _gpuPowerText = NotAvailable;
    private string _gpuVramText = NotAvailable;

    private DateTime? _lastBootUpTime;
    private PowerPlanInfo _selectedPowerPlan;
    private string _powerPlanStatusText = "";

    private int? _currentMemorySpeedMhz;
    private int? _ratedMemorySpeedMhz;
    private double? _rustDriveFreePercent;
    private bool _memorySizeWarningVisible;

    private IReadOnlyList<RamModuleRow> _ramModules = [];
    private IReadOnlyList<StorageDeviceRow> _storageDevices = [];
    private IReadOnlyList<PowerPlanInfo> _powerPlans = [];

    // Windows' actual out-of-the-box state for all three, so it's the least-wrong guess before
    // the real value loads.
    private bool _pointerPrecisionEnabled = true;
    private bool _gameModeEnabled = true;
    private bool _backgroundRecordingEnabled = true;
    private bool? _fullscreenOptimizationsDisabledForRust;
    private bool _medalRunning;

    /// <summary>Creates the view model and kicks off every async load for the page's sections.</summary>
    public SystemViewModel(ILocalizationService localization, ISystemInfoService systemInfo,
        ISystemTweaksService systemTweaks, IRustProcessService rustProcess)
        : base(localization)
    {
        _systemInfo = systemInfo;
        _systemTweaks = systemTweaks;
        _rustProcess = rustProcess;

        // StorageSpaceWarningTooltip is a formatted C# string, not a plain {Binding Localization[Key]}
        // lookup, so it needs to be manually re-raised on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                OnPropertyChanged(nameof(StorageSpaceWarningTooltip));
        };

        // Cheap and already resolved: name/OS description are cached strings, GetMemoryInfo() just
        // reads an already-open sensor. Everything else below is a WMI query or a process spawn,
        // so it's loaded asynchronously instead.
        CpuName = _systemInfo.GetCpuName();
        GpuName = _systemInfo.GetGpuName();
        OsDescription = _systemInfo.GetOsDescription();

        MemoryInfo memory = _systemInfo.GetMemoryInfo();
        RamText = FormatMemory(memory);
        RamUsagePercent = ComputeUsagePercent(memory);
        MemorySizeWarningVisible = !SystemOptimizationRecommendations.IsMemorySizeRecommended(memory.TotalBytes);

        // Each section loads independently so a slow one (GPU driver lookup can take seconds on
        // some machines) never blocks the others from appearing as soon as they're ready.
        _ = LoadMemorySpeedAsync();
        _ = LoadCpuDetailsAsync();
        _ = LoadMotherboardAsync();
        _ = LoadBiosAsync();
        _ = LoadOsDetailsAsync();
        _ = LoadGpuDriverVersionAsync();
        _ = LoadRamModulesAsync();
        _ = LoadStorageDevicesAsync();
        _ = LoadPowerPlansAsync();
        _ = LoadGamingTweaksAsync();
    }

    /// <summary>Loads current/rated RAM clock speed.</summary>
    private async Task LoadMemorySpeedAsync()
    {
        MemorySpeedInfo memorySpeed = await Task.Run(_systemInfo.GetMemorySpeedInfo);
        MemorySpeedText = FormatMemorySpeed(memorySpeed.CurrentMhz);
        MaxMemorySpeedText = FormatMemorySpeed(memorySpeed.RatedMhz);

        _currentMemorySpeedMhz = memorySpeed.CurrentMhz;
        _ratedMemorySpeedMhz = memorySpeed.RatedMhz;
        OnPropertyChanged(nameof(MemorySpeedWarningVisible));
    }

    /// <summary>Loads CPU core/thread count.</summary>
    private async Task LoadCpuDetailsAsync()
        => CpuCoresText = FormatCpuCores(await Task.Run(_systemInfo.GetCpuDetails));

    /// <summary>Loads motherboard manufacturer/model.</summary>
    private async Task LoadMotherboardAsync()
    {
        MotherboardInfo motherboard = await Task.Run(_systemInfo.GetMotherboardInfo);
        MotherboardText = $"{motherboard.Manufacturer} {motherboard.Model}".Trim();
    }

    /// <summary>Loads BIOS version and release date.</summary>
    private async Task LoadBiosAsync()
    {
        BiosInfo bios = await Task.Run(_systemInfo.GetBiosInfo);
        BiosText = bios.ReleaseDate is { } released ? $"{bios.Version} ({released:yyyy-MM-dd})" : bios.Version;
    }

    /// <summary>Loads OS build number, install date, and last boot time.</summary>
    private async Task LoadOsDetailsAsync()
    {
        OsDetails osDetails = await Task.Run(_systemInfo.GetOsDetails);
        OsBuildText = osDetails.BuildNumber is { } build ? $"Build {build}" : NotAvailable;
        OsInstallDateText = osDetails.InstallDate is { } installed ? installed.ToString("yyyy-MM-dd") : NotAvailable;
        _lastBootUpTime = osDetails.LastBootUpTime;
        OsUptimeText = FormatUptime(_lastBootUpTime);
    }

    /// <summary>Loads the GPU driver version.</summary>
    private async Task LoadGpuDriverVersionAsync()
        => GpuDriverVersionText = await Task.Run(_systemInfo.GetGpuDriverVersion) ?? NotAvailable;

    /// <summary>Loads installed RAM modules.</summary>
    private async Task LoadRamModulesAsync()
        => RamModules = (await Task.Run(_systemInfo.GetRamModules)).Select(ToRow).ToList();

    /// <summary>Loads physical storage devices and their drive letters, and resolves free space on Rust's drive.</summary>
    private async Task LoadStorageDevicesAsync()
        => ApplyStorageDevices(await Task.Run(_systemInfo.GetStorageDevices));

    /// <summary>Applies a storage devices reading to the bound rows and re-derives Rust's drive free-space warning.</summary>
    private void ApplyStorageDevices(IReadOnlyList<StorageDeviceInfo> devices)
    {
        StorageDevices = devices.Select(ToRow).ToList();

        _rustDriveFreePercent = SystemOptimizationRecommendations.FindRustDriveFreePercent(_rustProcess.GetInstallPath(), devices);
        OnPropertyChanged(nameof(StorageSpaceWarningVisible));
        OnPropertyChanged(nameof(StorageSpaceWarningTooltip));
    }

    /// <summary>Loads every power plan and selects the currently active one.</summary>
    private async Task LoadPowerPlansAsync()
    {
        IReadOnlyList<PowerPlanInfo> plans = await Task.Run(_systemTweaks.GetPowerPlans);
        PowerPlans = plans;
        _selectedPowerPlan = plans.FirstOrDefault(p => p.IsActive);
        OnPropertyChanged(nameof(SelectedPowerPlan));
        OnPropertyChanged(nameof(PowerPlanWarningVisible));
    }

    /// <summary>Loads the current state of every gaming tweak.</summary>
    private async Task LoadGamingTweaksAsync()
    {
        GamingTweaksSettings settings = await Task.Run(_systemTweaks.GetGamingTweaksSettings);

        _pointerPrecisionEnabled = settings.PointerPrecisionEnabled;
        OnPropertyChanged(nameof(PointerPrecisionEnabled));
        OnPropertyChanged(nameof(PointerPrecisionWarningVisible));

        _gameModeEnabled = settings.GameModeEnabled;
        OnPropertyChanged(nameof(GameModeEnabled));
        OnPropertyChanged(nameof(GameModeWarningVisible));

        _medalRunning = settings.MedalRunning;
        OnPropertyChanged(nameof(GameModeOverriddenByMedal));

        _backgroundRecordingEnabled = settings.BackgroundRecordingEnabled;
        OnPropertyChanged(nameof(BackgroundRecordingEnabled));
        OnPropertyChanged(nameof(BackgroundRecordingWarningVisible));

        _fullscreenOptimizationsDisabledForRust = settings.FullscreenOptimizationsDisabledForRust;
        OnPropertyChanged(nameof(FullscreenOptimizationsDisabledForRust));
        OnPropertyChanged(nameof(ShowFullscreenOptimizationsToggle));
        OnPropertyChanged(nameof(FullscreenOptimizationsWarningVisible));
    }

    /// <summary>The CPU's model name.</summary>
    public string CpuName
    {
        get => _cpuName;
        private set => SetProperty(ref _cpuName, value);
    }

    /// <summary>Formatted CPU core/thread count, e.g. "8 Cores / 16 Threads".</summary>
    public string CpuCoresText
    {
        get => _cpuCoresText;
        private set => SetProperty(ref _cpuCoresText, value);
    }

    /// <summary>The primary GPU's model name.</summary>
    public string GpuName
    {
        get => _gpuName;
        private set => SetProperty(ref _gpuName, value);
    }

    /// <summary>The GPU driver version.</summary>
    public string GpuDriverVersionText
    {
        get => _gpuDriverVersionText;
        private set => SetProperty(ref _gpuDriverVersionText, value);
    }

    /// <summary>A human-readable OS description, e.g. "Windows 11 64-bit".</summary>
    public string OsDescription
    {
        get => _osDescription;
        private set => SetProperty(ref _osDescription, value);
    }

    /// <summary>Formatted OS build number.</summary>
    public string OsBuildText
    {
        get => _osBuildText;
        private set => SetProperty(ref _osBuildText, value);
    }

    /// <summary>Formatted OS install date.</summary>
    public string OsInstallDateText
    {
        get => _osInstallDateText;
        private set => SetProperty(ref _osInstallDateText, value);
    }

    /// <summary>Formatted time since last boot, refreshed on every poll tick.</summary>
    public string OsUptimeText
    {
        get => _osUptimeText;
        private set => SetProperty(ref _osUptimeText, value);
    }

    /// <summary>Formatted RAM usage, e.g. "12.3 GB / 32 GB (38%)".</summary>
    public string RamText
    {
        get => _ramText;
        private set => SetProperty(ref _ramText, value);
    }

    /// <summary>Formatted current RAM clock speed.</summary>
    public string MemorySpeedText
    {
        get => _memorySpeedText;
        private set => SetProperty(ref _memorySpeedText, value);
    }

    /// <summary>Formatted rated (maximum) RAM clock speed.</summary>
    public string MaxMemorySpeedText
    {
        get => _maxMemorySpeedText;
        private set => SetProperty(ref _maxMemorySpeedText, value);
    }

    /// <summary>Whether the memory-speed warning icon should show - true once loaded and running below its rated speed (XMP/EXPO not enabled).</summary>
    public bool MemorySpeedWarningVisible =>
        _currentMemorySpeedMhz is { } current && _ratedMemorySpeedMhz is { } rated
        && !SystemOptimizationRecommendations.IsMemorySpeedRecommended(current, rated);

    /// <summary>Formatted CPU load percentage.</summary>
    public string CpuUsageText
    {
        get => _cpuUsageText;
        private set => SetProperty(ref _cpuUsageText, value);
    }

    /// <summary>Formatted GPU load percentage.</summary>
    public string GpuUsageText
    {
        get => _gpuUsageText;
        private set => SetProperty(ref _gpuUsageText, value);
    }

    /// <summary>0-100. Drives the CPU usage gauge's progress bar - <see cref="CpuUsageText"/> is what's actually displayed as text.</summary>
    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        private set => SetProperty(ref _cpuUsagePercent, value);
    }

    /// <summary>0-100. Drives the GPU usage gauge's progress bar - <see cref="GpuUsageText"/> is what's actually displayed as text.</summary>
    public double GpuUsagePercent
    {
        get => _gpuUsagePercent;
        private set => SetProperty(ref _gpuUsagePercent, value);
    }

    /// <summary>0-100. Drives the RAM usage gauge's progress bar - <see cref="RamText"/> is what's actually displayed as text.</summary>
    public double RamUsagePercent
    {
        get => _ramUsagePercent;
        private set => SetProperty(ref _ramUsagePercent, value);
    }

    /// <summary>Whether the memory-size warning icon should show - true when total RAM is below the recommended minimum.</summary>
    public bool MemorySizeWarningVisible
    {
        get => _memorySizeWarningVisible;
        private set => SetProperty(ref _memorySizeWarningVisible, value);
    }

    /// <summary>Formatted motherboard manufacturer and model.</summary>
    public string MotherboardText
    {
        get => _motherboardText;
        private set => SetProperty(ref _motherboardText, value);
    }

    /// <summary>Formatted BIOS version and release date.</summary>
    public string BiosText
    {
        get => _biosText;
        private set => SetProperty(ref _biosText, value);
    }

    /// <summary>Formatted GPU temperature.</summary>
    public string GpuTemperatureText
    {
        get => _gpuTemperatureText;
        private set => SetProperty(ref _gpuTemperatureText, value);
    }

    /// <summary>Formatted GPU core clock speed.</summary>
    public string GpuCoreClockText
    {
        get => _gpuCoreClockText;
        private set => SetProperty(ref _gpuCoreClockText, value);
    }

    /// <summary>Formatted GPU memory clock speed.</summary>
    public string GpuMemoryClockText
    {
        get => _gpuMemoryClockText;
        private set => SetProperty(ref _gpuMemoryClockText, value);
    }

    /// <summary>Formatted GPU fan speed.</summary>
    public string GpuFanText
    {
        get => _gpuFanText;
        private set => SetProperty(ref _gpuFanText, value);
    }

    /// <summary>Formatted GPU power draw.</summary>
    public string GpuPowerText
    {
        get => _gpuPowerText;
        private set => SetProperty(ref _gpuPowerText, value);
    }

    /// <summary>Formatted GPU VRAM used/total.</summary>
    public string GpuVramText
    {
        get => _gpuVramText;
        private set => SetProperty(ref _gpuVramText, value);
    }

    /// <summary>One row per installed RAM stick. Empty until its async load finishes.</summary>
    public IReadOnlyList<RamModuleRow> RamModules
    {
        get => _ramModules;
        private set => SetProperty(ref _ramModules, value);
    }

    /// <summary>One row per physical storage device. Empty until its async load finishes.</summary>
    public IReadOnlyList<StorageDeviceRow> StorageDevices
    {
        get => _storageDevices;
        private set => SetProperty(ref _storageDevices, value);
    }

    /// <summary>Whether the storage-space warning icon should show - true once resolved and Rust's drive is low on free space.</summary>
    public bool StorageSpaceWarningVisible =>
        _rustDriveFreePercent is { } freePercent && !SystemOptimizationRecommendations.IsStorageSpaceRecommended(freePercent);

    /// <summary>Formatted storage-space warning tooltip, naming the actual free-space percentage on Rust's drive.</summary>
    public string StorageSpaceWarningTooltip => string.Format(Localization["StorageSpaceWarningTooltipFormat"], _rustDriveFreePercent ?? 0);

    /// <summary>Every power plan Windows knows about. Empty until its async load finishes.</summary>
    public IReadOnlyList<PowerPlanInfo> PowerPlans
    {
        get => _powerPlans;
        private set => SetProperty(ref _powerPlans, value);
    }

    /// <summary>The currently active power plan. Setting it activates that plan.</summary>
    public PowerPlanInfo SelectedPowerPlan
    {
        get => _selectedPowerPlan;
        set
        {
            if (!SetProperty(ref _selectedPowerPlan, value) || string.IsNullOrEmpty(value.Id))
                return;

            OnPropertyChanged(nameof(PowerPlanWarningVisible));
            _ = ApplyPowerPlanAsync(value.Id);
        }
    }

    /// <summary>Whether the active power plan isn't High performance/Ultimate Performance, so the warning icon should show.</summary>
    public bool PowerPlanWarningVisible =>
        !string.IsNullOrEmpty(SelectedPowerPlan.Id) && !SystemOptimizationRecommendations.IsPowerPlanRecommended(SelectedPowerPlan.Id);

    /// <summary>Error message shown after a failed power plan switch; empty otherwise.</summary>
    public string PowerPlanStatusText
    {
        get => _powerPlanStatusText;
        private set => SetProperty(ref _powerPlanStatusText, value);
    }

    /// <summary>Activates the given power plan, off the UI thread since <c>powercfg.exe</c> is a process spawn.</summary>
    private async Task ApplyPowerPlanAsync(string planId)
    {
        bool success = await Task.Run(() => _systemTweaks.SetActivePowerPlan(planId));
        PowerPlanStatusText = success ? "" : Localization["PowerPlanChangeFailed"];
    }

    /// <summary>Whether mouse acceleration ("Enhance pointer precision") is enabled.</summary>
    public bool PointerPrecisionEnabled
    {
        get => _pointerPrecisionEnabled;
        set
        {
            if (!SetProperty(ref _pointerPrecisionEnabled, value))
                return;

            OnPropertyChanged(nameof(PointerPrecisionWarningVisible));
            _ = ApplyGamingTweakAsync(() => _systemTweaks.SetPointerPrecisionEnabled(value),
                () => _pointerPrecisionEnabled = !value, nameof(PointerPrecisionEnabled), nameof(PointerPrecisionWarningVisible));
        }
    }

    /// <summary>Whether the pointer-precision warning icon should show - true when it's not at its recommended value.</summary>
    public bool PointerPrecisionWarningVisible => !SystemOptimizationRecommendations.IsPointerPrecisionRecommended(PointerPrecisionEnabled);

    /// <summary>Whether Windows Game Mode is enabled.</summary>
    public bool GameModeEnabled
    {
        get => _gameModeEnabled;
        set
        {
            if (!SetProperty(ref _gameModeEnabled, value))
                return;

            OnPropertyChanged(nameof(GameModeWarningVisible));
            _ = ApplyGamingTweakAsync(() => _systemTweaks.SetGameModeEnabled(value), () => _gameModeEnabled = !value,
                nameof(GameModeEnabled), nameof(GameModeWarningVisible));
        }
    }

    /// <summary>Whether the Game Mode warning icon should show - true when it's not at its recommended value.</summary>
    public bool GameModeWarningVisible => !SystemOptimizationRecommendations.IsGameModeRecommended(GameModeEnabled);

    /// <summary>
    /// Whether Medal's clip recorder is running, which can set Game Mode back off itself a few
    /// seconds after login.
    /// </summary>
    public bool GameModeOverriddenByMedal => _medalRunning;

    /// <summary>Whether Xbox Game Bar's background recording (both the master switch and the instant-replay buffer) is enabled.</summary>
    public bool BackgroundRecordingEnabled
    {
        get => _backgroundRecordingEnabled;
        set
        {
            if (!SetProperty(ref _backgroundRecordingEnabled, value))
                return;

            OnPropertyChanged(nameof(BackgroundRecordingWarningVisible));
            _ = ApplyGamingTweakAsync(() => _systemTweaks.SetBackgroundRecordingEnabled(value),
                () => _backgroundRecordingEnabled = !value, nameof(BackgroundRecordingEnabled), nameof(BackgroundRecordingWarningVisible));
        }
    }

    /// <summary>Whether the background-recording warning icon should show - true when it's not at its recommended value.</summary>
    public bool BackgroundRecordingWarningVisible => !SystemOptimizationRecommendations.IsBackgroundRecordingRecommended(BackgroundRecordingEnabled);

    /// <summary>
    /// <see langword="null"/> until <see cref="LoadGamingTweaksAsync"/> resolves whether Rust is
    /// installed - the row stays hidden (see <see cref="ShowFullscreenOptimizationsToggle"/>) until then.
    /// </summary>
    public bool? FullscreenOptimizationsDisabledForRust
    {
        get => _fullscreenOptimizationsDisabledForRust;
        set
        {
            if (value is not { } disabled || !SetProperty(ref _fullscreenOptimizationsDisabledForRust, value))
                return;

            OnPropertyChanged(nameof(FullscreenOptimizationsWarningVisible));
            _ = ApplyGamingTweakAsync(() => _systemTweaks.SetFullscreenOptimizationsDisabledForRust(disabled),
                () => _fullscreenOptimizationsDisabledForRust = !disabled,
                nameof(FullscreenOptimizationsDisabledForRust), nameof(FullscreenOptimizationsWarningVisible));
        }
    }

    /// <summary>Whether Rust's install path could be resolved, so the fullscreen-optimizations row has something to act on.</summary>
    public bool ShowFullscreenOptimizationsToggle => _fullscreenOptimizationsDisabledForRust.HasValue;

    /// <summary>Whether the fullscreen-optimizations warning icon should show - true when applicable and not at its recommended value.</summary>
    public bool FullscreenOptimizationsWarningVisible =>
        FullscreenOptimizationsDisabledForRust is { } disabled && !SystemOptimizationRecommendations.IsFullscreenOptimizationsRecommended(disabled);

    /// <summary>Applies a gaming tweak off the UI thread, reverting the optimistic UI update (and re-notifying the given properties) if it fails.</summary>
    private async Task ApplyGamingTweakAsync(Func<bool> apply, Action revert, params string[] propertyNames)
    {
        bool success = await Task.Run(apply);
        if (!success)
        {
            revert();
            foreach (string propertyName in propertyNames)
                OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// Starts polling live usage every second. Call from the view's attach-to-visual-tree
    /// lifecycle so polling pauses while a different page is showing.
    /// </summary>
    public void StartPolling()
    {
        _ = PollAsync();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();
    }

    /// <summary>
    /// Re-reads every gaming tweak from the registry. The view model is cached across visits (see
    /// <c>MainWindowViewModel.Navigate</c>), so without this, a tweak changed outside the app - by
    /// Windows, Medal, or hand-editing the registry - would keep showing whatever this instance
    /// last knew. Call from the view's attach-to-visual-tree lifecycle so it's current on every visit.
    /// </summary>
    public void RefreshGamingTweaks() => _ = LoadGamingTweaksAsync();

    /// <summary>
    /// Stops polling. Call from the view's detach-from-visual-tree lifecycle.
    /// </summary>
    public void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    /// <summary>
    /// Reads CPU/RAM/GPU sensors off the UI thread and updates the bound text - batched into one
    /// <see cref="Task.Run(Action)"/> rather than three, since concurrent calls into the shared
    /// LibreHardwareMonitor <c>Computer</c> instance aren't known to be thread-safe. Every
    /// <see cref="StoragePollEveryNTicks"/>th tick also re-reads storage devices, so free space
    /// stays live-ish without re-enumerating physical disks every second.
    /// <see cref="_isPolling"/> skips a tick if the previous one is still running.
    /// </summary>
    private async Task PollAsync()
    {
        if (_isPolling)
            return;

        _isPolling = true;
        try
        {
            bool refreshStorage = ++_storagePollTickCount >= StoragePollEveryNTicks;
            if (refreshStorage)
                _storagePollTickCount = 0;

            (MemoryInfo memory, double? cpuPercent, GpuSensorInfo gpu, IReadOnlyList<StorageDeviceInfo>? storageDevices) = await Task.Run(() =>
                (_systemInfo.GetMemoryInfo(), _systemInfo.GetCpuUsagePercent(), _systemInfo.GetGpuSensors(),
                    refreshStorage ? _systemInfo.GetStorageDevices() : null));

            if (storageDevices is not null)
                ApplyStorageDevices(storageDevices);

            RamText = FormatMemory(memory);
            RamUsagePercent = ComputeUsagePercent(memory);
            CpuUsageText = FormatPercent(cpuPercent);
            CpuUsagePercent = cpuPercent ?? 0;
            OsUptimeText = FormatUptime(_lastBootUpTime);

            GpuUsageText = FormatPercent(gpu.LoadPercent);
            GpuUsagePercent = gpu.LoadPercent ?? 0;
            GpuTemperatureText = gpu.TemperatureC is { } temp ? $"{temp:0}°C" : NotAvailable;
            GpuCoreClockText = gpu.CoreClockMhz is { } coreClock ? $"{coreClock:0} MHz" : NotAvailable;
            GpuMemoryClockText = gpu.MemoryClockMhz is { } memClock ? $"{memClock:0} MHz" : NotAvailable;
            GpuFanText = gpu.FanRpm is { } fan ? $"{fan:0} RPM" : NotAvailable;
            GpuPowerText = gpu.PowerWatts is { } power ? $"{power:0.#} W" : NotAvailable;
            GpuVramText = gpu.VramUsedMiB is { } used && gpu.VramTotalMiB is { } total
                ? $"{used:0} MiB / {total:0} MiB"
                : NotAvailable;
        }
        finally
        {
            _isPolling = false;
        }
    }

    /// <summary>Formats used/total RAM and the usage percentage as one string.</summary>
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

    /// <summary>Formats a nullable percentage, or <see cref="NotAvailable"/> if unset.</summary>
    private static string FormatPercent(double? percent) => percent is { } value ? $"{value:0}%" : NotAvailable;

    /// <summary>Computes used/total as a 0-100 percentage.</summary>
    private static double ComputeUsagePercent(MemoryInfo memory) =>
        memory.TotalBytes == 0 ? 0 : memory.UsedBytes / (double)memory.TotalBytes * 100.0;

    /// <summary>Formats a nullable clock speed in MHz, or <see cref="NotAvailable"/> if unset.</summary>
    private static string FormatMemorySpeed(int? mhz) => mhz is { } value ? $"{value} MHz" : NotAvailable;

    /// <summary>Formats core/thread counts as one string, or <see cref="NotAvailable"/> if either is unknown.</summary>
    private static string FormatCpuCores(CpuDetails details) =>
        details is { CoreCount: { } cores, LogicalProcessorCount: { } threads }
            ? $"{cores} Cores / {threads} Threads"
            : NotAvailable;

    /// <summary>Formats the time elapsed since <paramref name="lastBoot"/>, or <see cref="NotAvailable"/> if unknown.</summary>
    private static string FormatUptime(DateTime? lastBoot)
    {
        if (lastBoot is not { } boot)
            return NotAvailable;

        TimeSpan uptime = DateTime.Now - boot;
        if (uptime < TimeSpan.Zero)
            return NotAvailable;

        return uptime.Days > 0
            ? $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m"
            : $"{uptime.Hours}h {uptime.Minutes}m";
    }

    /// <summary>Converts a RAM module reading into its display row.</summary>
    private static RamModuleRow ToRow(RamModuleInfo module)
    {
        double capacityGb = module.CapacityBytes / (1024.0 * 1024.0 * 1024.0);
        string speed = module.SpeedMhz is { } mhz ? $"{mhz} MHz" : NotAvailable;
        return new RamModuleRow(module.Slot, $"{capacityGb:0.#} GB", module.Manufacturer, speed);
    }

    /// <summary>Converts a storage device reading (with its drive letters) into its display row.</summary>
    private static StorageDeviceRow ToRow(StorageDeviceInfo device)
    {
        double capacityGb = device.CapacityBytes / (1024.0 * 1024.0 * 1024.0);
        IReadOnlyList<LogicalDriveRow> drives = device.Drives.Select(ToRow).ToList();
        return new StorageDeviceRow(device.Model, device.MediaType, $"{capacityGb:0} GB", drives);
    }

    /// <summary>Converts a logical drive reading into its display row.</summary>
    private static LogicalDriveRow ToRow(LogicalDriveInfo drive)
    {
        const double bytesPerGb = 1024.0 * 1024.0 * 1024.0;
        double usedGb = (drive.TotalBytes - drive.FreeBytes) / bytesPerGb;
        double totalGb = drive.TotalBytes / bytesPerGb;
        double percentUsed = drive.TotalBytes == 0
            ? 0
            : (drive.TotalBytes - drive.FreeBytes) / (double)drive.TotalBytes * 100.0;
        return new LogicalDriveRow(drive.Name, $"{usedGb:0.#} GB / {totalGb:0.#} GB used", percentUsed);
    }
}