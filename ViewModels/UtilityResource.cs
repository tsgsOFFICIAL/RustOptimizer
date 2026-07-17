using IconPacks.Avalonia.PhosphorIcons;
using System.Collections.Generic;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One external resource shown as a card on the Utilities page. <see cref="Name"/> is a brand/site
/// name and isn't translated; <see cref="DescriptionKey"/> is a localization key resolved by
/// <see cref="UtilitiesViewModel"/> into the display-ready <see cref="UtilityResourceRow"/>.
/// </summary>
public sealed record UtilityResource(string Name, string DescriptionKey, string Url, PackIconPhosphorIconsKind IconKind);

/// <summary>One resource, formatted for display in the Utilities page's resource cards.</summary>
public sealed record UtilityResourceRow(string Name, string Description, string Url, PackIconPhosphorIconsKind IconKind);

/// <summary>
/// A list of all the resources shown on the Utilities page.
/// </summary>
public static class UtilityResourceCatalog
{
    public static IReadOnlyList<UtilityResource> All { get; } =
    [
        new("RustHelp", "UtilityRustHelpDescription", "https://rusthelp.com", PackIconPhosphorIconsKind.BookOpen),
        new("RustBreeder", "UtilityRustBreederDescription", "https://rustbreeder.com", PackIconPhosphorIconsKind.Dna),
        new("BattleMetrics", "UtilityBattleMetricsDescription", "https://www.battlemetrics.com", PackIconPhosphorIconsKind.ChartBar),
        new("RustStats", "UtilityRustStatsDescription", "https://ruststats.io", PackIconPhosphorIconsKind.ChartBar),
        new("RustMaps", "UtilityRustMapsDescription", "https://rustmaps.com", PackIconPhosphorIconsKind.MapTrifold),
        new("Corrosion Hour", "UtilityCorrosionHourDescription", "https://corrosionhour.com", PackIconPhosphorIconsKind.Newspaper),
        new("Official Rust Wiki", "UtilityOfficialWikiDescription", "https://wiki.facepunch.com/rust", PackIconPhosphorIconsKind.BookBookmark),
        new("Official Rust Commits", "UtilityOfficialCommitsDescription", "https://commits.facepunch.com", PackIconPhosphorIconsKind.GitBranch),
        new("Rustrician", "UtilityRustricianDescription", "https://www.rustrician.io", PackIconPhosphorIconsKind.CarBattery),
        new("Raidboi-1129 (iOS)", "UtilityRaidboi1129Description", "https://apps.apple.com/us/app/raidboi-1129-rust-calculator/id6478394279", PackIconPhosphorIconsKind.AppleLogo),
        new("Raidboi-1129 (Android)", "UtilityRaidboi1129Description", "https://play.google.com/store/apps/details?id=com.wantapps.rust", PackIconPhosphorIconsKind.GooglePlayLogo)
    ];
}