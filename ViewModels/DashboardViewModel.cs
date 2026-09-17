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
/// Optimization Overview's four tiles (System/Network/Gameplay scored, Graphics a profile-match
/// badge) and the Smart Optimization hero button, via <see cref="ISmartOptimizationService"/>.
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
    private readonly ISmartOptimizationService _smartOptimization;
    private readonly IAppSettingsService _settings;
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
    private OptimizationCategoryScore _gameplayScore;
    private IReadOnlyList<string> _gameplayOutstandingLabelKeys = [];
    private DateTime? _lastScanTime;
    private string _smartOptimizationStatusText = "";

    // Keeps each tile's "what's wrong" summary compact - beyond this many, the rest are only a
    // click away on the tile's own full page anyway.
    private const int MaxIssuesShown = 2;

    /// <summary>Creates the view model, resolves the card's hardware identity strings once, and kicks off the System/Network scores' async loads.</summary>
    public DashboardViewModel(ILocalizationService localization, ISystemInfoService systemInfo, ISystemTweaksService systemTweaks,
        INetworkTweaksService networkTweaks, IRustProcessService rustProcess, IConfigService configService,
        ICleanupService cleanup, IDialogService dialogs, ISmartOptimizationService smartOptimization, IAppSettingsService settings, SidebarViewModel sidebar)
        : base(localization)
    {
        _rustProcess = rustProcess;
        _configService = configService;
        _systemTweaks = systemTweaks;
        _networkTweaks = networkTweaks;
        _systemInfo = systemInfo;
        _cleanup = cleanup;
        _dialogs = dialogs;
        _smartOptimization = smartOptimization;
        _settings = settings;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        // Persisted, so "Last scan" survives closing and reopening the app instead of resetting to "Never".
        _lastScanTime = _settings.Current.LastScanTime;

        // SystemIssuesSummaryText/NetworkIssuesSummaryText/GameplayIssuesSummaryText are built from
        // localized strings in C#, not a plain {Binding Localization[Key]} lookup, so they need to be
        // manually re-raised on language switch. GraphicsProfileStatusText/LastScanText read
        // Localization directly in their getters, so re-raising them just re-invokes those getters.
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
            {
                OnPropertyChanged(nameof(SystemIssuesSummaryText));
                OnPropertyChanged(nameof(NetworkIssuesSummaryText));
                OnPropertyChanged(nameof(GameplayIssuesSummaryText));
                OnPropertyChanged(nameof(GraphicsProfileStatusText));
                OnPropertyChanged(nameof(GraphicsProfileTagText));
                OnPropertyChanged(nameof(LastScanText));
            }
        };

        RunSmartOptimizationCommand = new RelayCommand(() => _ = RunSmartOptimizationAsync());

        VerifyRustFilesCommand = new RelayCommand(VerifyRustFiles);
        OptimizeStartupCommand = new RelayCommand(OptimizeStartup);
        ClearCacheCommand = new RelayCommand(() => _ = ClearCacheAsync());
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        ViewSystemDetailsCommand = new RelayCommand(() => SystemDetailsRequested?.Invoke(this, EventArgs.Empty));
        ViewNetworkDetailsCommand = new RelayCommand(() => NetworkDetailsRequested?.Invoke(this, EventArgs.Empty));
        ViewGameplayDetailsCommand = new RelayCommand(() => GameplayDetailsRequested?.Invoke(this, EventArgs.Empty));
        ManageProfilesCommand = new RelayCommand(() => ManageProfilesRequested?.Invoke(this, EventArgs.Empty));
        UpdateDriversCommand = new RelayCommand(() => _ = _dialogs.ShowUpdateDriversAsync(Localization, _systemInfo));

        CpuName = systemInfo.GetCpuName();
        GpuName = systemInfo.GetGpuName();
        OsDescription = systemInfo.GetOsDescription();
        RamText = FormatTotalMemory(systemInfo.GetInstalledMemoryBytes());

        IsRustInstalled = _rustProcess.GetInstallPath() != null;
    }

    /// <summary>
    /// Raised when the "More Details" row in the System Information card is clicked, so the shell
    /// can navigate to the full System page.
    /// </summary>
    public event EventHandler? SystemDetailsRequested;

    /// <summary>Builds and, on confirmation, applies a Smart Optimization plan. See <see cref="RunSmartOptimizationAsync"/>.</summary>
    public RelayCommand RunSmartOptimizationCommand { get; }

    /// <summary>Verifies Rust's game files via Steam.</summary>
    public RelayCommand VerifyRustFilesCommand { get; }

    /// <summary>Opens Task Manager, so the user can review and disable their own startup apps.</summary>
    public RelayCommand OptimizeStartupCommand { get; }

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

    /// <summary>
    /// Raised when the Optimization Overview's Gameplay tile is clicked, so the shell can navigate
    /// to the full Gameplay page.
    /// </summary>
    public event EventHandler? GameplayDetailsRequested;

    /// <summary>Raises <see cref="GameplayDetailsRequested"/> to navigate to the Gameplay page.</summary>
    public RelayCommand ViewGameplayDetailsCommand { get; }

    /// <summary>
    /// Raised when the "Manage Profiles" row on the Preset Profiles card is clicked, so the shell can
    /// navigate to the Graphics page where profiles are created and switched.
    /// </summary>
    public event EventHandler? ManageProfilesRequested;

    /// <summary>Raises <see cref="ManageProfilesRequested"/> to navigate to the Graphics page.</summary>
    public RelayCommand ManageProfilesCommand { get; }

    /// <summary>Shows the "Update Drivers" prompt.</summary>
    public RelayCommand UpdateDriversCommand { get; }

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
    /// The Gameplay category's optimization tally for the Optimization Overview - how many of the
    /// "recommended for everyone" Gameplay tweaks are currently applied, scored via
    /// <see cref="GameplayOptimizationRecommendations"/>. Zero/zero until <see cref="LoadGameplayScoreAsync"/> finishes.
    /// </summary>
    public OptimizationCategoryScore GameplayScore
    {
        get => _gameplayScore;
        private set => SetProperty(ref _gameplayScore, value);
    }

    /// <summary>
    /// A short, comma-separated preview of which "recommended for everyone" Gameplay tweaks aren't
    /// applied yet, or "" once every one of them is. Capped at <see cref="MaxIssuesShown"/>, same as
    /// <see cref="SystemIssuesSummaryText"/>.
    /// </summary>
    public string GameplayIssuesSummaryText => BuildIssuesSummaryText(_gameplayOutstandingLabelKeys);

    /// <summary>
    /// The Graphics tile's badge text: the matched built-in preset's name, <c>"Custom"</c> if
    /// client.cfg matches none of them, or <see cref="NotAvailable"/> if Rust isn't installed.
    /// Unlike System/Network/Gameplay, graphics quality has no single "correct" value, so this is a
    /// status badge rather than a numeric score - see <see cref="ISmartOptimizationService"/>'s own
    /// reasoning for never treating a graphics preset as right/wrong.
    /// </summary>
    public string GraphicsProfileStatusText
    {
        get
        {
            if (!IsRustInstalled)
                return NotAvailable;

            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.LowEndPc))
                return Localization["ProfileLowEndPc"];
            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.Competitive))
                return Localization["ProfileCompetitive"];
            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.Cinematic))
                return Localization["ProfileCinematic"];

            return Localization["GraphicsProfileCustom"];
        }
    }

    /// <summary>
    /// The matched built-in preset's own descriptive tag ("High FPS"/"Recommended"/"Best Quality" -
    /// the same tags the Preset Profiles card shows next to each preset), or "" for Custom/not
    /// installed. Fills the Graphics tile's second line so it carries the same visual weight as the
    /// scored tiles' progress bar row, without inventing a fake score for it.
    /// </summary>
    public string GraphicsProfileTagText
    {
        get
        {
            if (!IsRustInstalled)
                return "";

            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.LowEndPc))
                return Localization["TagHighFps"];
            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.Competitive))
                return Localization["TagRecommended"];
            if (_configService.CurrentConfigMatchesPreset(ConfigPreset.Cinematic))
                return Localization["TagBestQuality"];

            return "";
        }
    }

    /// <summary>
    /// The hero card's "Last scan" value - "Never" until the first Smart Optimization run this
    /// session, then just the time ("2:59 PM") for a scan from earlier today, "Yesterday, 2:59 PM"
    /// for one from the day before, or a full date for anything older - so a stale scan doesn't
    /// read as if it just happened.
    /// </summary>
    public string LastScanText
    {
        get
        {
            if (_lastScanTime is not { } time)
                return Localization["LastScanNever"];

            string timePart = time.ToString("t");
            DateTime today = DateTime.Now.Date;

            if (time.Date == today)
                return timePart;

            return time.Date == today.AddDays(-1)
                ? $"{Localization["Yesterday"]}, {timePart}"
                : $"{time:yyyy-MM-dd}, {timePart}";
        }
    }

    /// <summary>Status line shown under the Smart Optimization button after a run - "already optimized", what was applied, or that some changes couldn't complete.</summary>
    public string SmartOptimizationStatusText
    {
        get => _smartOptimizationStatusText;
        private set => SetProperty(ref _smartOptimizationStatusText, value);
    }

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

    /// <summary>
    /// Re-fetches <see cref="GameplayScore"/>. Call whenever the Dashboard becomes visible again -
    /// tweaks made on the Gameplay page while this view model sat cached wouldn't otherwise be reflected.
    /// </summary>
    public void RefreshGameplayScore() => _ = LoadGameplayScoreAsync();

    /// <summary>Re-raises <see cref="GraphicsProfileStatusText"/>/<see cref="GraphicsProfileTagText"/> so they re-read client.cfg. Call whenever the Dashboard becomes visible again, same as the score refreshes.</summary>
    public void RefreshGraphicsProfileStatus()
    {
        OnPropertyChanged(nameof(GraphicsProfileStatusText));
        OnPropertyChanged(nameof(GraphicsProfileTagText));
    }

    /// <summary>Loads the Gameplay score off the UI thread, independent of whether the Gameplay page itself has ever been visited.</summary>
    private async Task LoadGameplayScoreAsync()
    {
        (IReadOnlyList<GameplayTweak> tweaks, IReadOnlyDictionary<string, string> current) = await Task.Run(() =>
        {
            IReadOnlyList<GameplayTweak> t = _configService.GetRecommendedGameplayTweaks();
            IReadOnlyDictionary<string, string> c = _configService.ReadConvars(t.SelectMany(tweak => tweak.Convars.Select(cv => cv.Convar)).ToList());
            return (t, c);
        });

        GameplayOptimizationInputs inputs = new(tweaks, current);
        GameplayScore = GameplayOptimizationRecommendations.Score(inputs);
        _gameplayOutstandingLabelKeys = GameplayOptimizationRecommendations.GetOutstandingLabelKeys(inputs);
        OnPropertyChanged(nameof(GameplayIssuesSummaryText));
    }

    /// <summary>
    /// Builds a Smart Optimization plan - updating "Last scan" immediately, since that reflects
    /// when the system was last checked, not when something was last applied - and, unless
    /// there's nothing outstanding, shows the confirm prompt and applies it on confirmation,
    /// refreshing every tile. Shared logic with the Optimizer page's own button lives in
    /// <see cref="ISmartOptimizationService"/>, so both drive identical behavior.
    /// </summary>
    private async Task RunSmartOptimizationAsync()
    {
        SmartOptimizationPlan plan = await Task.Run(_smartOptimization.BuildPlan);

        // "Last scan" tracks when the system was last checked, not when something was last
        // applied - so it updates here, before either early return, rather than only after a
        // successful apply. Persisted immediately so it survives closing the app, not just kept
        // in memory for the session.
        _lastScanTime = DateTime.Now;
        _settings.Current.LastScanTime = _lastScanTime;
        _settings.Save();
        OnPropertyChanged(nameof(LastScanText));

        if (plan.IsEmpty)
        {
            SmartOptimizationStatusText = Localization["OptimizerAlreadyOptimizedText"];
            return;
        }

        if (await _dialogs.ShowSmartOptimizationAsync(Localization, _smartOptimization, plan) is not { } outcome)
            return;

        RefreshSystemScore();
        RefreshNetworkScore();
        RefreshGameplayScore();
        RefreshGraphicsProfileStatus();

        SmartOptimizationStatusText = Localization[HasPartialFailure(plan, outcome) ? "SmartOptimizationPartialText" : "SmartOptimizationAppliedText"];
    }

    /// <summary>
    /// Whether anything the plan asked for didn't actually happen - a requested Network batch that
    /// failed or was declined, or a requested Gameplay/Graphics write that failed (most commonly
    /// because Rust was running at apply time, which <see cref="IConfigService.SetConvars"/> always
    /// refuses). Drives whether the post-run status reads "applied" or "partially applied".
    /// </summary>
    private static bool HasPartialFailure(SmartOptimizationPlan plan, SmartOptimizationOutcome outcome)
    {
        bool networkFailed = outcome.NetworkChangesRequested > 0 && (!outcome.NetworkChangesApplied || outcome.NetworkElevationCancelled);
        bool gameplayFailed = plan.Changes.Any(c => c.Category == OptimizationCategory.Gameplay) && !outcome.GameplayChangesApplied;
        bool graphicsFailed = plan.RecommendedGraphicsPreset is not null && !outcome.GraphicsPresetApplied;

        return networkFailed || gameplayFailed || graphicsFailed;
    }

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
        (GamingTweaksSettings gaming, IReadOnlyList<PowerPlanInfo> plans, ulong installedMemoryBytes, MemorySpeedInfo memorySpeed, IReadOnlyList<StorageDeviceInfo> storageDevices, DisplayModeInfo display)
            = await Task.Run(() => (_systemTweaks.GetGamingTweaksSettings(), _systemTweaks.GetPowerPlans(),
                _systemInfo.GetInstalledMemoryBytes(), _systemInfo.GetMemorySpeedInfo(), _systemInfo.GetStorageDevices(), _systemInfo.GetDisplayModeInfo()));

        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        double? rustDriveFreePercent = SystemOptimizationRecommendations.FindRustDriveFreePercent(_rustProcess.GetInstallPath(), storageDevices);
        SystemOptimizationInputs inputs = new(gaming, activePlanId, installedMemoryBytes, memorySpeed, rustDriveFreePercent, display);

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

    /// <summary>
    /// Opens Task Manager. There's no supported way to land it directly on the Startup Apps tab or
    /// pre-sorted by impact - column sort and tab selection are UI state Windows doesn't expose a hook
    /// for - so this just gets the user to the right tool rather than faking control we don't have.
    /// </summary>
    private void OptimizeStartup()
    {
        Utility.OpenUrl("taskmgr.exe");
    }

    /// <summary>Applies the preset profile named by <paramref name="tag"/>.</summary>
    private void ApplyPreset(string? tag)
    {
        if (!CanApplyPreset || !Enum.TryParse(tag, out ConfigPreset preset))
            return;

        bool success = _configService.ApplyPreset(preset);
        PresetStatusText = Localization[success ? "PresetApplied" : "PresetApplyFailed"];

        if (success)
            RefreshGraphicsProfileStatus();
    }

    /// <summary>Formats total installed RAM capacity, or <see cref="NotAvailable"/> if unknown.</summary>
    private static string FormatTotalMemory(ulong installedBytes)
    {
        if (installedBytes == 0)
            return NotAvailable;

        const double bytesPerGb = 1024.0 * 1024.0 * 1024.0;
        double totalGb = installedBytes / bytesPerGb;
        return $"{totalGb:0.#} GB";
    }
}