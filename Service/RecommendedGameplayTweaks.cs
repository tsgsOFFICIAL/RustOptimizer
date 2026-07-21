using System.Collections.Generic;
using RustOptimizer.Interface;
using static RustOptimizer.Interface.GameplayTweakCategory;

namespace RustOptimizer.Service;

/// <summary>
/// Every tweak shown on the Gameplay page: pure visibility/clarity convar changes with no
/// performance cost, as opposed to <see cref="RustConfigPresets"/>'s graphics/performance presets.
/// Values and defaults were verified against an actual client.cfg and
/// https://pages.rusthelp.com/tools/admin/command-list.
/// </summary>
internal static class RecommendedGameplayTweaks
{
    public static IReadOnlyList<GameplayTweak> All { get; } =
    [
        new([new ConvarValue("client.crosshair", "False", "True")],
            "TweakCrosshairLabel", "TweakCrosshairDescription", RecommendedForEveryone),

        new([
                new ConvarValue("effects.maxgibs", "-1", "1000"),
                new ConvarValue("effects.maxgibdist", "0", "150")
            ],
            "TweakMaxGibsLabel", "TweakMaxGibsDescription", RecommendedForEveryone),
        
        new([new ConvarValue("accessibility.treemarkercolor", "2", "0")],
            "TweakTreeMarkerColorLabel", "TweakTreeMarkerColorDescription", RecommendedForEveryone),

        new([new ConvarValue("client.headbob", "False", "True")],
            "TweakHeadbobLabel", "TweakHeadbobDescription", RecommendedForEveryone),

        new([new ConvarValue("client.hurtpunch", "False", "True")],
            "TweakHurtpunchLabel", "TweakHurtpunchDescription", RecommendedForEveryone),

        new([
                new ConvarValue("effects.hurtoverlay", "False", "True"),
                new ConvarValue("effects.hurtoverleyapplylighting", "False", "True")
            ],
            "TweakHurtOverlayLabel", "TweakHurtOverlayDescription", RecommendedForEveryone),

        new([new ConvarValue("effects.lensdirt", "False", "True")],
            "TweakLensDirtLabel", "TweakLensDirtDescription", RecommendedForEveryone),

        new([new ConvarValue("graphics.fov", "90", "75")],
            "TweakFovLabel", "TweakFovDescription", RecommendedForEveryone),

        new([new ConvarValue("ui.worldnotifications", "1", "0")],
            "TweakWorldNotificationsLabel", "TweakWorldNotificationsDescription", Preference),

        new([new ConvarValue("effects.vignet", "False", "True")],
            "TweakVignetteLabel", "TweakVignetteDescription", Preference),
        
        new([new ConvarValue("accessibility.holosightcolour", "1", "0")],
            "TweakHolosightColourLabel", "TweakHolosightColourDescription", Preference),

        new([new ConvarValue("effects.sharpen", "True", "False")],
            "TweakSharpenLabel", "TweakSharpenDescription", Preference),

        new([new ConvarValue("graphics.vm_fov_scale", "False", "True")],
            "TweakVmFovScaleLabel", "TweakVmFovScaleDescription", Preference),
    ];
}