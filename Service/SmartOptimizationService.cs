using RustOptimizer.Service.Logging;
using System.Collections.Generic;
using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace RustOptimizer.Service;

/// <inheritdoc cref="ISmartOptimizationService" />
[SupportedOSPlatform("windows")]
public sealed class SmartOptimizationService(
    ISystemTweaksService systemTweaks,
    INetworkTweaksService networkTweaks,
    IConfigService configService,
    IConfigBackupService configBackup,
    ISystemInfoService systemInfo,
    IRustProcessService rustProcess) : ISmartOptimizationService
{
    /// <inheritdoc />
    public SmartOptimizationPlan BuildPlan()
    {
        // Detecting what's outstanding is a safe read regardless of whether Rust is running - only
        // writing Gameplay/Graphics changes needs the guard (see ApplyAsync and
        // SmartOptimizationPlan.RequiresClosingRust). Reporting them here rather than hiding them
        // lets the confirm dialog tell the user to close Rust instead of misleadingly claiming
        // there's nothing outstanding.
        List<PlannedChange> changes = [];
        changes.AddRange(BuildSystemChanges(systemTweaks.GetGamingTweaksSettings(), systemTweaks.GetPowerPlans()));
        changes.AddRange(BuildNetworkChanges(networkTweaks.GetNetworkTweaksSettings()));
        changes.AddRange(BuildGameplayChanges());

        (ConfigPreset? preset, string? reasonKey, double memoryGb, int? refreshRateHz) = BuildGraphicsRecommendation();

        return new SmartOptimizationPlan(changes, preset, reasonKey, memoryGb, refreshRateHz, rustProcess.IsRunning());
    }

    /// <inheritdoc />
    public async Task<SmartOptimizationOutcome> ApplyAsync(SmartOptimizationPlan plan)
    {
        // Re-checked here, not just trusted from when the plan was built - Rust could have been
        // launched in the gap between confirming the dialog and clicking Apply. Same guard as
        // BuildPlan; ConfigService.SetConvars/ApplyPreset would refuse anyway, but checking here
        // skips the pointless backup/attempt entirely instead of relying on that inner refusal.
        bool rustRunning = rustProcess.IsRunning();

        // One backup covers every client.cfg write this run makes (preset and/or gameplay tweaks),
        // rather than one per write - mirrors why GameplayViewModel's own master toggle skips
        // backups for a routine, reversible change, just scoped to the whole run instead of one row.
        bool touchesConfig = !rustRunning && (plan.RecommendedGraphicsPreset is not null || plan.Changes.Any(c => c.Category == OptimizationCategory.Gameplay));
        if (touchesConfig)
            configBackup.CreateBackup(ConfigBackupType.Settings, label: null);

        int systemApplied = plan.Changes.Any(c => c.Category == OptimizationCategory.System) ? ApplySystemChanges() : 0;
        bool gameplayApplied = !rustRunning && plan.Changes.Any(c => c.Category == OptimizationCategory.Gameplay) && ApplyGameplayChanges();
        bool graphicsApplied = !rustRunning && plan.RecommendedGraphicsPreset is { } preset && configService.ApplyPreset(preset, createBackup: false);

        List<PlannedChange> networkChanges = plan.Changes.Where(c => c.Category == OptimizationCategory.Network).ToList();
        (bool networkApplied, bool elevationCancelled) = networkChanges.Count > 0
            ? await ApplyNetworkChangesAsync(networkChanges)
            : (false, false);

        return new SmartOptimizationOutcome(systemApplied, gameplayApplied, graphicsApplied,
            networkChanges.Count, networkApplied, elevationCancelled);
    }

    /// <summary>Every System tweak not currently at its recommended value - the informational-only checks (RAM size, memory speed, storage, refresh rate, resolution) have no setter and are deliberately left out.</summary>
    private static IReadOnlyList<PlannedChange> BuildSystemChanges(GamingTweaksSettings gaming, IReadOnlyList<PowerPlanInfo> plans)
    {
        List<PlannedChange> changes = [];

        if (!SystemOptimizationRecommendations.IsPointerPrecisionRecommended(gaming.PointerPrecisionEnabled))
            changes.Add(new PlannedChange(OptimizationCategory.System, "PointerPrecisionLabel", RequiresElevation: false));

        if (!SystemOptimizationRecommendations.IsGameModeRecommended(gaming.GameModeEnabled))
            changes.Add(new PlannedChange(OptimizationCategory.System, "GameModeLabel", false));

        if (!SystemOptimizationRecommendations.IsBackgroundRecordingRecommended(gaming.BackgroundRecordingEnabled))
            changes.Add(new PlannedChange(OptimizationCategory.System, "BackgroundRecordingLabel", false));

        if (gaming.FullscreenOptimizationsDisabledForRust is { } disabled && !SystemOptimizationRecommendations.IsFullscreenOptimizationsRecommended(disabled))
            changes.Add(new PlannedChange(OptimizationCategory.System, "FullscreenOptimizationsLabel", false));

        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        if (activePlanId is not null && !SystemOptimizationRecommendations.IsPowerPlanRecommended(activePlanId))
            changes.Add(new PlannedChange(OptimizationCategory.System, "PowerPlanTitle", false));

        return changes;
    }

    /// <summary>Every Network tweak not currently at its recommended value - wired-vs-wireless has no setter (the app can't rewire the house) and is deliberately left out, same as System's informational-only checks.</summary>
    private static IReadOnlyList<PlannedChange> BuildNetworkChanges(NetworkTweaksSettings settings)
    {
        List<PlannedChange> changes = [];

        if (settings.NetworkThrottlingDisabled is { } throttling && !NetworkOptimizationRecommendations.IsNetworkThrottlingRecommended(throttling))
            changes.Add(new PlannedChange(OptimizationCategory.Network, "NetworkThrottlingLabel", RequiresElevation: true));

        if (settings.NicPowerSavingDisabled is { } nicPowerSaving && !NetworkOptimizationRecommendations.IsNicPowerSavingRecommended(nicPowerSaving))
            changes.Add(new PlannedChange(OptimizationCategory.Network, "NicPowerSavingLabel", true));

        if (settings.QosReservedBandwidthDisabled is { } qos && !NetworkOptimizationRecommendations.IsQosReservedBandwidthRecommended(qos))
            changes.Add(new PlannedChange(OptimizationCategory.Network, "QosReservedBandwidthLabel", true));

        return changes;
    }

    /// <summary>Every "recommended for everyone" Gameplay tweak not currently applied.</summary>
    private IReadOnlyList<PlannedChange> BuildGameplayChanges()
    {
        IReadOnlyList<GameplayTweak> tweaks = configService.GetRecommendedGameplayTweaks();
        IReadOnlyDictionary<string, string> current = configService.ReadConvars(
            tweaks.SelectMany(t => t.Convars.Select(c => c.Convar)).ToList());

        return tweaks
            .Where(t => t.Category == GameplayTweakCategory.RecommendedForEveryone && !GameplayOptimizationRecommendations.IsTweakApplied(t, current))
            .Select(t => new PlannedChange(OptimizationCategory.Gameplay, t.LabelKey, RequiresElevation: false))
            .ToList();
    }

    /// <summary>
    /// Recommends a graphics preset only when Rust is installed and client.cfg matches none of the
    /// three built-in presets yet - a deliberately chosen preset (even a non-recommended one) is
    /// left alone rather than silently switched.
    /// </summary>
    private (ConfigPreset? Preset, string? ReasonKey, double MemoryGb, int? RefreshRateHz) BuildGraphicsRecommendation()
    {
        if (rustProcess.GetInstallPath() is null)
            return (null, null, 0, null);

        if (Enum.GetValues<ConfigPreset>().Any(configService.CurrentConfigMatchesPreset))
            return (null, null, 0, null);

        RecommendedPreset recommendation = GraphicsPresetRecommendation.Recommend(
            systemInfo.GetInstalledMemoryBytes(), systemInfo.GetDisplayModeInfo().MaxHz);

        return (recommendation.Preset, recommendation.ReasonKey, recommendation.InstalledMemoryGb, recommendation.MaxRefreshRateHz);
    }

    /// <summary>Applies every outstanding System tweak directly (no elevation needed), counting only the ones that actually succeed.</summary>
    private int ApplySystemChanges()
    {
        int applied = 0;
        GamingTweaksSettings gaming = systemTweaks.GetGamingTweaksSettings();

        if (!SystemOptimizationRecommendations.IsPointerPrecisionRecommended(gaming.PointerPrecisionEnabled) && systemTweaks.SetPointerPrecisionEnabled(true))
            applied++;

        if (!SystemOptimizationRecommendations.IsGameModeRecommended(gaming.GameModeEnabled) && systemTweaks.SetGameModeEnabled(true))
            applied++;

        if (!SystemOptimizationRecommendations.IsBackgroundRecordingRecommended(gaming.BackgroundRecordingEnabled) && systemTweaks.SetBackgroundRecordingEnabled(false))
            applied++;

        if (gaming.FullscreenOptimizationsDisabledForRust is { } disabled
            && !SystemOptimizationRecommendations.IsFullscreenOptimizationsRecommended(disabled)
            && systemTweaks.SetFullscreenOptimizationsDisabledForRust(true))
            applied++;

        IReadOnlyList<PowerPlanInfo> plans = systemTweaks.GetPowerPlans();
        string? activePlanId = plans.FirstOrDefault(p => p.IsActive).Id;
        if (activePlanId is not null && !SystemOptimizationRecommendations.IsPowerPlanRecommended(activePlanId))
        {
            PowerPlanInfo recommendedPlan = plans.FirstOrDefault(p => SystemOptimizationRecommendations.IsPowerPlanRecommended(p.Id));
            if (recommendedPlan.Id is not null && systemTweaks.SetActivePowerPlan(recommendedPlan.Id))
                applied++;
        }

        return applied;
    }

    /// <summary>Writes every outstanding "recommended for everyone" Gameplay tweak to client.cfg in one call.</summary>
    private bool ApplyGameplayChanges()
    {
        IReadOnlyList<GameplayTweak> tweaks = configService.GetRecommendedGameplayTweaks();
        IReadOnlyDictionary<string, string> current = configService.ReadConvars(
            tweaks.SelectMany(t => t.Convars.Select(c => c.Convar)).ToList());

        Dictionary<string, string> convars = new(StringComparer.OrdinalIgnoreCase);
        foreach (GameplayTweak tweak in tweaks.Where(t => t.Category == GameplayTweakCategory.RecommendedForEveryone && !GameplayOptimizationRecommendations.IsTweakApplied(t, current)))
            foreach (ConvarValue convar in tweak.Convars)
                convars[convar.Convar] = convar.EnabledValue;

        return convars.Count > 0 && configService.SetConvars(convars, createBackup: false);
    }

    /// <summary>
    /// Applies every outstanding Network tweak in one elevated re-launch - one UAC prompt for the
    /// whole batch, unlike the Network page's own per-toggle elevation. Re-reads current state
    /// right before building the batch, since some time may have passed since the plan was built.
    /// </summary>
    private async Task<(bool Applied, bool ElevationCancelled)> ApplyNetworkChangesAsync(IReadOnlyList<PlannedChange> plannedNetworkChanges)
    {
        NetworkTweaksSettings settings = networkTweaks.GetNetworkTweaksSettings();
        IReadOnlyList<PlannedChange> stillOutstanding = BuildNetworkChanges(settings)
            .Where(pending => plannedNetworkChanges.Any(planned => planned.LabelKey == pending.LabelKey))
            .ToList();

        if (stillOutstanding.Count == 0)
            return (true, false);

        List<string> args = ["--apply-network-tweaks"];
        foreach (PlannedChange change in stillOutstanding)
        {
            string key = change.LabelKey switch
            {
                "NetworkThrottlingLabel" => "NetworkThrottling",
                "NicPowerSavingLabel" => "NicPowerSaving",
                "QosReservedBandwidthLabel" => "QosReservedBandwidth",
                _ => null!
            };

            if (key is null)
                continue;

            args.Add(key);
            args.Add("1");
        }

        ElevatedRunResult result = await Task.Run(() => ElevationHelper.RunElevated([.. args]));
        if (result == ElevatedRunResult.CancelledByUser)
            return (false, true);

        if (result != ElevatedRunResult.Success)
        {
            AppLog.Warn("SmartOptimizationService", "Elevated network tweak batch reported failure.");
            return (false, false);
        }

        return (true, false);
    }
}