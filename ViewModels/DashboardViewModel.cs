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
/// Optimization Overview's System and Network tiles, scored from the same tweaks their own pages use.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IRustProcessService _rustProcess;
    private readonly IConfigService _configService;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly INetworkTweaksService _networkTweaks;
    private readonly ISystemInfoService _systemInfo;
    private readonly ICleanupService _cleanup;
    private readonly IDialogService _dialogs;
    private readonly SidebarViewModel _sidebar;
    private const string NotAvailable = "N/A";

    // Joins the parts of the post-cleanup status line ("Freed 4.2 GB · 12 files in use were skipped").
    private const string StatusPartSeparator = " · ";

    private string _cpuName = "";
    private string _gpuName = "";
    private string _osDescription = "";
    private string _ramText = NotAvailable;
    private string _presetStatusText = "";
    private string _clearCacheStatusText = "";
    private bool _isClearingCache;
    private bool _isRustInstalled = true;
    private OptimizationCategoryScore _systemScore;
    private IReadOnlyList<string> _systemOutstandingLabelKeys = [];
    private OptimizationCategoryScore _networkScore;
    private IReadOnlyList<string> _networkOutstandingLabelKeys = [];

    // Keeps each tile's "what's wrong" summary compact - beyond this many, the rest are only a
    // click away on the tile's own full page anyway.
    private const int MaxIssuesShown = 2;

    /// <summary>Creates the view model, resolves the card's hardware identity strings once, and kicks off the System/Network scores' async loads.</summary>
    public DashboardViewModel(ILocalizationService localization, ISystemInfoService systemInfo, ISystemTweaksService systemTweaks,
        INetworkTweaksService networkTweaks, IRustProcessService rustProcess, IConfigService configService,
        ICleanupService cleanup, IDialogService dialogs, SidebarViewModel sidebar)
        : base(localization)
    {
        _rustProcess = rustProcess;
        _configService = configService;
        _systemTweaks = systemTweaks;
        _networkTweaks = networkTweaks;
        _systemInfo = systemInfo;
        _cleanup = cleanup;
        _dialogs = dialogs;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        // SystemIssuesSummaryText/NetworkIssuesSummaryText are built from localized strings in C#,
        // not a plain {Binding Localization[Key]} lookup, so they need to be manually re-raised on language switch.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
            {
                OnPropertyChanged(nameof(SystemIssuesSummaryText));
                OnPropertyChanged(nameof(NetworkIssuesSummaryText));
            }
        };

        RunSmartOptimizationCommand = new RelayCommand(() =>
        {
            // Mock data only for now - no real optimization logic wired up yet.
        });

        VerifyRustFilesCommand = new RelayCommand(VerifyRustFiles);
        ClearCacheCommand = new RelayCommand(() => _ = ClearCacheAsync());
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        ViewSystemDetailsCommand = new RelayCommand(() => SystemDetailsRequested?.Invoke(this, EventArgs.Empty));
        ViewNetworkDetailsCommand = new RelayCommand(() => NetworkDetailsRequested?.Invoke(this, EventArgs.Empty));

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

    /// <summary>Prompts for cleanup options, then clears the caches the user left enabled.</summary>
    public RelayCommand ClearCacheCommand { get; }

    /// <summary>Applies the preset profile named by its parameter.</summary>
    public RelayCommand<string> ApplyPresetCommand { get; }

    /// <summary>Raises <see cref="SystemDetailsRequested"/> to navigate to the System page.</summary>
    public RelayCommand ViewSystemDetailsCommand { get; }

    /// <summary>
    /// Raised when the Optimization Overview's Network tile is clicked, so the shell can navigate
    /// to the full Network page.
    /// </summary>
    public event EventHandler? NetworkDetailsRequested;

    /// <summary>Raises <see cref="NetworkDetailsRequested"/> to navigate to the Network page.</summary>
    public RelayCommand ViewNetworkDetailsCommand { get; }

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

    /// <summary>
    /// Status message shown under the Clear Cache button - "Clearing…" while a run is in progress,
    /// then what it freed. Empty until the first run finishes.
    /// </summary>
    public string ClearCacheStatusText
    {
        get => _clearCacheStatusText;
        private set => SetProperty(ref _clearCacheStatusText, value);
    }

    /// <summary>Whether a cleanup is currently running, which disables the button for its duration.</summary>
    public bool IsClearingCache
    {
        get => _isClearingCache;
        private set
        {
            if (SetProperty(ref _isClearingCache, value))
                OnPropertyChanged(nameof(CanClearCache));
        }
    }

    /// <summary>Whether the Clear Cache button should be enabled - false only while a run is in progress.</summary>
    public bool CanClearCache => !IsClearingCache;

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
    /// <see cref="LoadSystemScoreAsync"/> finishes.
    /// </summary>
    public OptimizationCategoryScore SystemScore
    {
        get => _systemScore;
        private set => SetProperty(ref _systemScore, value);
    }

    /// <summary>
    /// A short, comma-separated preview of what's not optimized yet (e.g. "Game Mode, Power Plan
    /// +1 more"), or "" once every applicable check passes. Capped at <see cref="MaxIssuesShown"/>
    /// so the tile stays compact - the System page itself lists every check with its own warning icon.
    /// </summary>
    public string SystemIssuesSummaryText => BuildIssuesSummaryText(_systemOutstandingLabelKeys);

    /// <summary>
    /// The Network category's optimization tally for the Optimization Overview - how many of the
    /// Network page's recommended tweaks are currently applied, scored via
    /// <see cref="NetworkOptimizationRecommendations"/> so both pages agree. Zero/zero until
    /// <see cref="LoadNetworkScoreAsync"/> finishes.
    /// </summary>
    public OptimizationCategoryScore NetworkScore
    {
        get => _networkScore;
        private set => SetProperty(ref _networkScore, value);
    }

    /// <summary>
    /// A short, comma-separated preview of what's not optimized yet on the Network page, or "" once
    /// every applicable check passes. Capped at <see cref="MaxIssuesShown"/>, same as <see cref="SystemIssuesSummaryText"/>.
    /// </summary>
    public string NetworkIssuesSummaryText => BuildIssuesSummaryText(_networkOutstandingLabelKeys);

    /// <summary>
    /// Builds a tile's "what's wrong" summary from its outstanding check label keys, capped at
    /// <see cref="MaxIssuesShown"/> - shared by <see cref="SystemIssuesSummaryText"/> and
    /// <see cref="NetworkIssuesSummaryText"/> so both tiles read identically.
    /// </summary>
    private string BuildIssuesSummaryText(IReadOnlyList<string> outstandingLabelKeys)
    {
        if (outstandingLabelKeys.Count == 0)
            return "";

        string shown = string.Join(", ", outstandingLabelKeys.Take(MaxIssuesShown).Select(key => Localization[key]));
        int remaining = outstandingLabelKeys.Count - MaxIssuesShown;

        return remaining > 0 ? string.Format(Localization["IssuesMoreFormat"], shown, remaining) : shown;
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

    /// <summary>
    /// Re-fetches <see cref="NetworkScore"/>. Call whenever the Dashboard becomes visible again -
    /// tweaks made on the Network page while this view model sat cached wouldn't otherwise be reflected.
    /// </summary>
    public void RefreshNetworkScore() => _ = LoadNetworkScoreAsync();

    /// <summary>Loads the Network score off the UI thread, independent of whether the Network page itself has ever been visited.</summary>
    private async Task LoadNetworkScoreAsync()
    {
        (NetworkTweaksSettings settings, NetworkAdapterInfo? adapterInfo) = await Task.Run(() =>
            (_networkTweaks.GetNetworkTweaksSettings(), _networkTweaks.GetActiveAdapterInfo()));

        NetworkOptimizationInputs inputs = new(settings.NetworkThrottlingDisabled, settings.NicPowerSavingDisabled,
            settings.QosReservedBandwidthDisabled, adapterInfo?.IsWireless);

        NetworkScore = NetworkOptimizationRecommendations.Score(inputs);
        _networkOutstandingLabelKeys = NetworkOptimizationRecommendations.GetOutstandingLabelKeys(inputs);
        OnPropertyChanged(nameof(NetworkIssuesSummaryText));
    }

    /// <summary>Loads the System score off the UI thread, independent of whether the System page itself has ever been visited.</summary>
    private async Task LoadSystemScoreAsync()
    {
        (GamingTweaksSettings gaming, IReadOnlyList<PowerPlanInfo> plans, MemorySpeedInfo memorySpeed, IReadOnlyList<StorageDeviceInfo> storageDevices, DisplayModeInfo display)
            = await Task.Run(() => (_systemTweaks.GetGamingTweaksSettings(), _systemTweaks.GetPowerPlans(),
                _systemInfo.GetMemorySpeedInfo(), _systemInfo.GetStorageDevices(), _systemInfo.GetDisplayModeInfo()));

        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        double? rustDriveFreePercent = SystemOptimizationRecommendations.FindRustDriveFreePercent(_rustProcess.GetInstallPath(), storageDevices);
        SystemOptimizationInputs inputs = new(gaming, activePlanId, _systemInfo.GetMemoryInfo(), memorySpeed, rustDriveFreePercent, display);

        SystemScore = SystemOptimizationRecommendations.Score(inputs);
        _systemOutstandingLabelKeys = SystemOptimizationRecommendations.GetOutstandingLabelKeys(inputs);
        OnPropertyChanged(nameof(SystemIssuesSummaryText));
    }

    /// <summary>
    /// Prompts for options and, unless cancelled, runs the cleanup. The prompt is the only gate:
    /// once the user confirms, the run isn't interruptible, so the button is disabled for its duration.
    /// </summary>
    private async Task ClearCacheAsync()
    {
        if (IsClearingCache)
            return;

        IsClearingCache = true;

        try
        {
            // The prompt runs the cleanup itself and stays open for its duration, so what comes back
            // is the finished outcome rather than a set of options to act on.
            if (await _dialogs.ShowClearCacheAsync(Localization, _cleanup) is { } outcome)
                ClearCacheStatusText = BuildClearCacheStatusText(outcome);
        }
        finally
        {
            IsClearingCache = false;
        }
    }

    /// <summary>
    /// Builds the post-cleanup status line: what was freed, plus a note for anything deliberately
    /// left alone (locked files, a running Steam or Rust, or a declined UAC prompt) so a smaller
    /// than expected total is explained rather than mysterious.
    /// </summary>
    private string BuildClearCacheStatusText(CleanupOutcome outcome)
    {
        List<string> parts =
        [
            outcome.BytesFreed > 0
                ? string.Format(Localization["ClearCacheFreedFormat"], FormatBytes(outcome.BytesFreed))
                : Localization["ClearCacheNothingFreed"]
        ];

        if (outcome.FilesSkipped > 0)
            parts.Add(string.Format(Localization["ClearCacheSkippedFormat"], outcome.FilesSkipped));

        if (outcome.RustWasRunning)
            parts.Add(Localization["ClearCacheRustSkipped"]);

        if (outcome.ElevationDeclined)
            parts.Add(Localization["ClearCacheSystemFilesSkipped"]);

        if (outcome.Cancelled)
            parts.Add(Localization["ClearCacheCancelled"]);

        return string.Join(StatusPartSeparator, parts);
    }

    /// <summary>
    /// Formats a byte count for display, e.g. "4.2 GB" or "812 MB". Unit symbols are left untranslated,
    /// matching how RAM and storage sizes are already rendered elsewhere in the app.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        const double kb = 1024.0;
        const double mb = kb * 1024.0;
        const double gb = mb * 1024.0;

        return bytes switch
        {
            >= (long)gb => $"{bytes / gb:0.#} GB",
            >= (long)mb => $"{bytes / mb:0.#} MB",
            _ => $"{bytes / kb:0} KB"
        };
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