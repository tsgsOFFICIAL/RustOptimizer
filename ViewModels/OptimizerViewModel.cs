using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Controls;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Optimizer page: the same Smart Optimization action as the Dashboard's hero button
/// (shared via <see cref="ISmartOptimizationService"/>, so both behave identically), plus a full,
/// untruncated view of every category's outstanding checks - the Dashboard's own tiles cap their
/// "what's wrong" preview at a couple of items and link out to each page for the rest;
/// this page just shows all of it directly.
/// </summary>
public sealed class OptimizerViewModel : ViewModelBase
{
    private readonly ISmartOptimizationService _smartOptimization;
    private readonly IDialogService _dialogs;
    private readonly ISystemTweaksService _systemTweaks;
    private readonly INetworkTweaksService _networkTweaks;
    private readonly IConfigService _configService;
    private readonly ISystemInfoService _systemInfo;
    private readonly IRustProcessService _rustProcess;

    private const string NotAvailable = "N/A";

    private OptimizationCategoryScore _systemScore;
    private IReadOnlyList<string> _systemOutstandingItems = [];
    private OptimizationCategoryScore _networkScore;
    private IReadOnlyList<string> _networkOutstandingItems = [];
    private OptimizationCategoryScore _gameplayScore;
    private IReadOnlyList<string> _gameplayOutstandingItems = [];
    private bool _isRustInstalled;
    private ConfigPreset? _recommendedGraphicsPreset;
    private string _statusText = "";

    /// <summary>Creates the view model and kicks off every category's score load.</summary>
    public OptimizerViewModel(ILocalizationService localization, ISmartOptimizationService smartOptimization, IDialogService dialogs,
        ISystemTweaksService systemTweaks, INetworkTweaksService networkTweaks, IConfigService configService,
        ISystemInfoService systemInfo, IRustProcessService rustProcess, SidebarViewModel sidebar)
        : base(localization)
    {
        _smartOptimization = smartOptimization;
        _dialogs = dialogs;
        _systemTweaks = systemTweaks;
        _networkTweaks = networkTweaks;
        _configService = configService;
        _systemInfo = systemInfo;
        _rustProcess = rustProcess;

        RunSmartOptimizationCommand = new RelayCommand(() => _ = RunSmartOptimizationAsync());
        ViewSystemPageCommand = new RelayCommand(() => sidebar.NavigateTo(SidebarPage.System));
        ViewNetworkPageCommand = new RelayCommand(() => sidebar.NavigateTo(SidebarPage.Network));
        ViewGameplayPageCommand = new RelayCommand(() => sidebar.NavigateTo(SidebarPage.Gameplay));
        ViewGraphicsPageCommand = new RelayCommand(() => sidebar.NavigateTo(SidebarPage.Graphics));

        RefreshAll();
    }

    /// <summary>Builds and, on confirmation, applies a Smart Optimization plan - identical behavior to the Dashboard's own button.</summary>
    public RelayCommand RunSmartOptimizationCommand { get; }

    /// <summary>Navigates to the System page.</summary>
    public RelayCommand ViewSystemPageCommand { get; }

    /// <summary>Navigates to the Network page.</summary>
    public RelayCommand ViewNetworkPageCommand { get; }

    /// <summary>Navigates to the Gameplay page.</summary>
    public RelayCommand ViewGameplayPageCommand { get; }

    /// <summary>Navigates to the Graphics page.</summary>
    public RelayCommand ViewGraphicsPageCommand { get; }

    /// <summary>The System category's optimization tally, scored via <see cref="SystemOptimizationRecommendations"/>.</summary>
    public OptimizationCategoryScore SystemScore
    {
        get => _systemScore;
        private set => SetProperty(ref _systemScore, value);
    }

    /// <summary>Every System check that isn't at its recommended value yet, fully localized (no truncation, unlike the Dashboard's tile).</summary>
    public IReadOnlyList<string> SystemOutstandingItems
    {
        get => _systemOutstandingItems;
        private set
        {
            if (SetProperty(ref _systemOutstandingItems, value))
                OnPropertyChanged(nameof(SystemOutstandingText));
        }
    }

    /// <summary>One bullet line per outstanding System check, newline-joined for direct display.</summary>
    public string SystemOutstandingText => BulletText(SystemOutstandingItems);

    /// <summary>The Network category's optimization tally, scored via <see cref="NetworkOptimizationRecommendations"/>.</summary>
    public OptimizationCategoryScore NetworkScore
    {
        get => _networkScore;
        private set => SetProperty(ref _networkScore, value);
    }

    /// <summary>Every Network check that isn't at its recommended value yet, fully localized.</summary>
    public IReadOnlyList<string> NetworkOutstandingItems
    {
        get => _networkOutstandingItems;
        private set
        {
            if (SetProperty(ref _networkOutstandingItems, value))
                OnPropertyChanged(nameof(NetworkOutstandingText));
        }
    }

    /// <summary>One bullet line per outstanding Network check, newline-joined for direct display.</summary>
    public string NetworkOutstandingText => BulletText(NetworkOutstandingItems);

    /// <summary>The Gameplay category's optimization tally - "recommended for everyone" tweaks only, scored via <see cref="GameplayOptimizationRecommendations"/>.</summary>
    public OptimizationCategoryScore GameplayScore
    {
        get => _gameplayScore;
        private set => SetProperty(ref _gameplayScore, value);
    }

    /// <summary>Every "recommended for everyone" Gameplay tweak that isn't applied yet, fully localized.</summary>
    public IReadOnlyList<string> GameplayOutstandingItems
    {
        get => _gameplayOutstandingItems;
        private set
        {
            if (SetProperty(ref _gameplayOutstandingItems, value))
                OnPropertyChanged(nameof(GameplayOutstandingText));
        }
    }

    /// <summary>One bullet line per outstanding "recommended for everyone" Gameplay tweak, newline-joined for direct display.</summary>
    public string GameplayOutstandingText => BulletText(GameplayOutstandingItems);

    /// <summary>The Graphics tile's badge text - see <see cref="DashboardViewModel.GraphicsProfileStatusText"/> for why this is a badge, not a score.</summary>
    public string GraphicsProfileStatusText
    {
        get
        {
            if (!_isRustInstalled)
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

    /// <summary>The matched built-in preset's own descriptive tag - see <see cref="DashboardViewModel.GraphicsProfileTagText"/>.</summary>
    public string GraphicsProfileTagText
    {
        get
        {
            if (!_isRustInstalled)
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
    /// Whether Smart Optimization currently has a graphics preset to recommend - only when nothing
    /// recognized is already applied, see <see cref="SmartOptimizationPlan.RecommendedGraphicsPreset"/>.
    /// </summary>
    public bool HasGraphicsRecommendation => _recommendedGraphicsPreset is not null;

    /// <summary>The recommended preset's display name, shown plainly on this page rather than only inside the confirm dialog.</summary>
    public string GraphicsRecommendationPresetText => _recommendedGraphicsPreset switch
    {
        ConfigPreset.LowEndPc => Localization["ProfileLowEndPc"],
        ConfigPreset.Competitive => Localization["ProfileCompetitive"],
        ConfigPreset.Cinematic => Localization["ProfileCinematic"],
        _ => ""
    };

    /// <summary>One-line explanation of why that preset was picked.</summary>
    public string GraphicsRecommendationReasonText { get; private set; } = "";

    /// <summary>Status line under the Smart Optimization button - mirrors <see cref="DashboardViewModel.SmartOptimizationStatusText"/>.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Re-reads every category's state. Call whenever the Optimizer page becomes visible again.</summary>
    public void RefreshAll()
    {
        _ = LoadSystemScoreAsync();
        _ = LoadNetworkScoreAsync();
        _ = LoadGameplayScoreAsync();
        _ = LoadGraphicsRecommendationAsync();
        _isRustInstalled = _rustProcess.GetInstallPath() != null;
        OnPropertyChanged(nameof(GraphicsProfileStatusText));
        OnPropertyChanged(nameof(GraphicsProfileTagText));
    }

    /// <summary>
    /// Loads the graphics-preset recommendation (if any) off the UI thread, reusing
    /// <see cref="ISmartOptimizationService.BuildPlan"/> - the same computation the confirm dialog
    /// uses, so this page and that dialog can never disagree on what's recommended or why.
    /// </summary>
    private async Task LoadGraphicsRecommendationAsync()
    {
        SmartOptimizationPlan plan = await Task.Run(_smartOptimization.BuildPlan);

        _recommendedGraphicsPreset = plan.RecommendedGraphicsPreset;
        GraphicsRecommendationReasonText = plan.GraphicsPresetReasonKey is { } key
            ? string.Format(Localization[key], plan.GraphicsInstalledMemoryGb, plan.GraphicsMaxRefreshRateHz ?? 0)
            : "";

        OnPropertyChanged(nameof(HasGraphicsRecommendation));
        OnPropertyChanged(nameof(GraphicsRecommendationPresetText));
        OnPropertyChanged(nameof(GraphicsRecommendationReasonText));
    }

    /// <summary>Loads the System score and its full outstanding-items list off the UI thread.</summary>
    private async Task LoadSystemScoreAsync()
    {
        (GamingTweaksSettings gaming, IReadOnlyList<PowerPlanInfo> plans, ulong installedMemoryBytes, MemorySpeedInfo memorySpeed, IReadOnlyList<StorageDeviceInfo> storageDevices, DisplayModeInfo display)
            = await Task.Run(() => (_systemTweaks.GetGamingTweaksSettings(), _systemTweaks.GetPowerPlans(),
                _systemInfo.GetInstalledMemoryBytes(), _systemInfo.GetMemorySpeedInfo(), _systemInfo.GetStorageDevices(), _systemInfo.GetDisplayModeInfo()));

        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        double? rustDriveFreePercent = SystemOptimizationRecommendations.FindRustDriveFreePercent(_rustProcess.GetInstallPath(), storageDevices);
        SystemOptimizationInputs inputs = new(gaming, activePlanId, installedMemoryBytes, memorySpeed, rustDriveFreePercent, display);

        SystemScore = SystemOptimizationRecommendations.Score(inputs);
        SystemOutstandingItems = SystemOptimizationRecommendations.GetOutstandingLabelKeys(inputs).Select(key => Localization[key]).ToList();
    }

    /// <summary>Loads the Network score and its full outstanding-items list off the UI thread.</summary>
    private async Task LoadNetworkScoreAsync()
    {
        (NetworkTweaksSettings settings, NetworkAdapterInfo? adapterInfo) = await Task.Run(() =>
            (_networkTweaks.GetNetworkTweaksSettings(), _networkTweaks.GetActiveAdapterInfo()));

        NetworkOptimizationInputs inputs = new(settings.NetworkThrottlingDisabled, settings.NicPowerSavingDisabled,
            settings.QosReservedBandwidthDisabled, adapterInfo?.IsWireless);

        NetworkScore = NetworkOptimizationRecommendations.Score(inputs);
        NetworkOutstandingItems = NetworkOptimizationRecommendations.GetOutstandingLabelKeys(inputs).Select(key => Localization[key]).ToList();
    }

    /// <summary>Loads the Gameplay score and its full outstanding-items list off the UI thread.</summary>
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
        GameplayOutstandingItems = GameplayOptimizationRecommendations.GetOutstandingLabelKeys(inputs).Select(key => Localization[key]).ToList();
    }

    /// <summary>Joins localized items into one bullet line per item, or "" if there are none.</summary>
    private static string BulletText(IReadOnlyList<string> items) => string.Join(Environment.NewLine, items.Select(item => $"• {item}"));

    /// <summary>Builds a Smart Optimization plan and, unless there's nothing outstanding, shows the confirm prompt and applies it on confirmation.</summary>
    private async Task RunSmartOptimizationAsync()
    {
        SmartOptimizationPlan plan = await Task.Run(_smartOptimization.BuildPlan);

        if (plan.IsEmpty)
        {
            StatusText = Localization["OptimizerAlreadyOptimizedText"];
            return;
        }

        if (await _dialogs.ShowSmartOptimizationAsync(Localization, _smartOptimization, plan) is not { } outcome)
            return;

        RefreshAll();

        StatusText = Localization[HasPartialFailure(plan, outcome) ? "SmartOptimizationPartialText" : "SmartOptimizationAppliedText"];
    }

    /// <summary>
    /// Whether anything the plan asked for didn't actually happen - a requested Network batch that
    /// failed or was declined, or a requested Gameplay/Graphics write that failed (most commonly
    /// because Rust was running at apply time, which <see cref="IConfigService.SetConvars"/> always
    /// refuses).
    /// </summary>
    private static bool HasPartialFailure(SmartOptimizationPlan plan, SmartOptimizationOutcome outcome)
    {
        bool networkFailed = outcome.NetworkChangesRequested > 0 && (!outcome.NetworkChangesApplied || outcome.NetworkElevationCancelled);
        bool gameplayFailed = plan.Changes.Any(c => c.Category == OptimizationCategory.Gameplay) && !outcome.GameplayChangesApplied;
        bool graphicsFailed = plan.RecommendedGraphicsPreset is not null && !outcome.GraphicsPresetApplied;

        return networkFailed || gameplayFailed || graphicsFailed;
    }
}