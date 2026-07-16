using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Dashboard's "System Information" card: hardware identity strings only, resolved
/// once - live usage and deeper detail live on the full System page, reachable via
/// <see cref="SystemDetailsRequested"/>. Rust's running state is tracked by <see cref="SidebarViewModel"/>
/// (always visible, already polling it) rather than polled again here. Also drives the
/// Optimization Overview's System tile, scored from the same tweaks the System page uses.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IRustProcessService _rustProcess;
    private readonly IConfigService _configService;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly ISystemInfoService _systemInfo;
    private readonly SidebarViewModel _sidebar;
    private const string NotAvailable = "N/A";

    private string _cpuName = "";
    private string _gpuName = "";
    private string _osDescription = "";
    private string _ramText = NotAvailable;
    private string _presetStatusText = "";
    private bool _isRustInstalled = true;
    private OptimizationCategoryScore _systemScore;
    private IReadOnlyList<string> _systemOutstandingLabelKeys = [];

    // Keeps the System tile's "what's wrong" summary compact - beyond this many, the rest are
    // only a click away on the full System page anyway.
    private const int MaxSystemIssuesShown = 2;

    /// <summary>Creates the view model, resolves the card's hardware identity strings once, and kicks off the System score's async load.</summary>
    public DashboardViewModel(ILocalizationService localization, ISystemInfoService systemInfo, ISystemTweaksService systemTweaks,
        IRustProcessService rustProcess, IConfigService configService, SidebarViewModel sidebar)
        : base(localization)
    {
        _rustProcess = rustProcess;
        _configService = configService;
        _systemTweaks = systemTweaks;
        _systemInfo = systemInfo;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        // SystemIssuesSummaryText is built from localized strings in C#, not a plain
        // {Binding Localization[Key]} lookup, so it needs to be manually re-raised on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                OnPropertyChanged(nameof(SystemIssuesSummaryText));
        };

        RunSmartOptimizationCommand = new RelayCommand(() =>
        {
            // Mock data only for now - no real optimization logic wired up yet.
        });

        VerifyRustFilesCommand = new RelayCommand(VerifyRustFiles);
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        ViewSystemDetailsCommand = new RelayCommand(() => SystemDetailsRequested?.Invoke(this, EventArgs.Empty));

        CpuName = systemInfo.GetCpuName();
        GpuName = systemInfo.GetGpuName();
        OsDescription = systemInfo.GetOsDescription();
        RamText = FormatTotalMemory(systemInfo.GetMemoryInfo());

        IsRustInstalled = _rustProcess.GetInstallPath() != null;
    }

    /// <summary>
    /// Raised when the "More Details" row in the System Information card is clicked, so the shell
    /// can navigate to the full System page.
    /// </summary>
    public event EventHandler? SystemDetailsRequested;

    /// <summary>Placeholder command for the not-yet-implemented smart optimization feature.</summary>
    public RelayCommand RunSmartOptimizationCommand { get; }

    /// <summary>Verifies Rust's game files via Steam.</summary>
    public RelayCommand VerifyRustFilesCommand { get; }

    /// <summary>Applies the preset profile named by its parameter.</summary>
    public RelayCommand<string> ApplyPresetCommand { get; }

    /// <summary>Raises <see cref="SystemDetailsRequested"/> to navigate to the System page.</summary>
    public RelayCommand ViewSystemDetailsCommand { get; }

    /// <summary>The CPU's model name.</summary>
    public string CpuName
    {
        get => _cpuName;
        private set => SetProperty(ref _cpuName, value);
    }

    /// <summary>The primary GPU's model name.</summary>
    public string GpuName
    {
        get => _gpuName;
        private set => SetProperty(ref _gpuName, value);
    }

    /// <summary>A human-readable OS description, e.g. "Windows 11 64-bit".</summary>
    public string OsDescription
    {
        get => _osDescription;
        private set => SetProperty(ref _osDescription, value);
    }

    /// <summary>Formatted total RAM capacity.</summary>
    public string RamText
    {
        get => _ramText;
        private set => SetProperty(ref _ramText, value);
    }

    /// <summary>Status message shown after applying a preset profile.</summary>
    public string PresetStatusText
    {
        get => _presetStatusText;
        private set => SetProperty(ref _presetStatusText, value);
    }

    /// <summary>Whether Rust's install path could be resolved.</summary>
    public bool IsRustInstalled
    {
        get => _isRustInstalled;
        set
        {
            if (SetProperty(ref _isRustInstalled, value))
            {
                OnPropertyChanged(nameof(CanVerifyRustFiles));
                OnPropertyChanged(nameof(CanApplyPreset));
            }
        }
    }

    /// <summary>
    /// Whether "Verify Game Files" should be enabled - Rust has to be installed, and closed, since
    /// verifying/repairing its files while the game has them open wouldn't work.
    /// </summary>
    public bool CanVerifyRustFiles => IsRustInstalled && !_sidebar.IsRustRunning;

    /// <summary>
    /// Whether preset profiles can be applied - there's no config to write to without a Rust
    /// install, so the Preset Profiles buttons stay disabled until one is found.
    /// </summary>
    public bool CanApplyPreset => IsRustInstalled;

    /// <summary>
    /// The System category's optimization tally for the Optimization Overview - how many of the
    /// System page's recommended settings are currently applied, scored via
    /// <see cref="SystemOptimizationRecommendations"/> so both pages agree. Zero/zero until
    /// <see cref="LoadSystemScoreAsync"/> finishes. The same <see cref="OptimizationCategoryScore"/>
    /// type is meant to back Performance/Network/Graphics too, once those pages have real checks
    /// of their own to score.
    /// </summary>
    public OptimizationCategoryScore SystemScore
    {
        get => _systemScore;
        private set => SetProperty(ref _systemScore, value);
    }

    /// <summary>
    /// A short, comma-separated preview of what's not optimized yet (e.g. "Game Mode, Power Plan
    /// +1 more"), or "" once every applicable check passes. Capped at <see cref="MaxSystemIssuesShown"/>
    /// so the tile stays compact - the System page itself lists every check with its own warning icon.
    /// </summary>
    public string SystemIssuesSummaryText
    {
        get
        {
            if (_systemOutstandingLabelKeys.Count == 0)
                return "";

            string shown = string.Join(", ", _systemOutstandingLabelKeys.Take(MaxSystemIssuesShown).Select(key => Localization[key]));
            int remaining = _systemOutstandingLabelKeys.Count - MaxSystemIssuesShown;

            return remaining > 0 ? string.Format(Localization["SystemIssuesMoreFormat"], shown, remaining) : shown;
        }
    }

    /// <summary>Re-evaluates <see cref="CanVerifyRustFiles"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanVerifyRustFiles));
    }

    /// <summary>
    /// Re-fetches <see cref="SystemScore"/>. Call whenever the Dashboard becomes visible again -
    /// tweaks made on the System page while this view model sat cached wouldn't otherwise be reflected.
    /// </summary>
    public void RefreshSystemScore() => _ = LoadSystemScoreAsync();

    /// <summary>Loads the System score off the UI thread, independent of whether the System page itself has ever been visited.</summary>
    private async Task LoadSystemScoreAsync()
    {
        (GamingTweaksSettings gaming, IReadOnlyList<PowerPlanInfo> plans, MemorySpeedInfo memorySpeed, IReadOnlyList<StorageDeviceInfo> storageDevices)
            = await Task.Run(() => (_systemTweaks.GetGamingTweaksSettings(), _systemTweaks.GetPowerPlans(),
                _systemInfo.GetMemorySpeedInfo(), _systemInfo.GetStorageDevices()));

        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        double? rustDriveFreePercent = SystemOptimizationRecommendations.FindRustDriveFreePercent(_rustProcess.GetInstallPath(), storageDevices);
        SystemOptimizationInputs inputs = new(gaming, activePlanId, _systemInfo.GetMemoryInfo(), memorySpeed, rustDriveFreePercent);

        SystemScore = SystemOptimizationRecommendations.Score(inputs);
        _systemOutstandingLabelKeys = SystemOptimizationRecommendations.GetOutstandingLabelKeys(inputs);
        OnPropertyChanged(nameof(SystemIssuesSummaryText));
    }

    /// <summary>Triggers Steam's file verification for Rust.</summary>
    private void VerifyRustFiles()
    {
        _rustProcess.VerifyFiles();
    }

    /// <summary>Applies the preset profile named by <paramref name="tag"/>.</summary>
    private void ApplyPreset(string? tag)
    {
        if (!CanApplyPreset || !Enum.TryParse(tag, out ConfigPreset preset))
            return;

        bool success = _configService.ApplyPreset(preset);
        PresetStatusText = Localization[success ? "PresetApplied" : "PresetApplyFailed"];
    }

    /// <summary>Formats total RAM capacity, or <see cref="NotAvailable"/> if unknown.</summary>
    private static string FormatTotalMemory(MemoryInfo memory)
    {
        if (memory.TotalBytes == 0)
            return NotAvailable;

        const double bytesPerGb = 1024.0 * 1024.0 * 1024.0;
        double totalGb = memory.TotalBytes / bytesPerGb;
        return $"{totalGb:0.#} GB";
    }
}