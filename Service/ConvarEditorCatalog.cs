using System.Collections.Generic;

namespace RustOptimizer.Service;

/// <summary>How a <see cref="ConvarEditorEntry"/>'s value is edited in the manual macro builder.</summary>
public enum ConvarEditorKind
{
    /// <summary>A numeric range, edited with a slider (<see cref="ConvarEditorEntry.Min"/>/<see cref="ConvarEditorEntry.Max"/>/<see cref="ConvarEditorEntry.Step"/>).</summary>
    Slider,

    /// <summary>An on/off switch, writing <see cref="ConvarEditorEntry.ToggleOnValue"/> or <see cref="ConvarEditorEntry.ToggleOffValue"/>.</summary>
    Toggle,

    /// <summary>Free text wrapped into <c>showtoast 3 "&lt;text&gt;"</c>.</summary>
    Text,
}

/// <summary>
/// One convar the manual macro builder can edit with a friendly control instead of raw text.
/// <see cref="ConvarKey"/> is the literal console command name written verbatim into the line - never
/// re-parsed or validated beyond that, same opaque-string discipline as everywhere else in this
/// feature.
/// </summary>
public readonly record struct ConvarEditorEntry(
    string ConvarKey,
    string LabelKey,
    ConvarEditorKind Kind,
    double Min = 0,
    double Max = 1,
    double Step = 0.1,
    string ToggleOnValue = "1",
    string ToggleOffValue = "0");

/// <summary>
/// The small set of convars actually seen in <see cref="RustActionCatalog"/>'s own <c>meta.exec</c>
/// macros - scoped to what's provably real, same rule as <see cref="RustActionCatalog"/> itself.
/// <c>graphics.fov</c>'s 70-90 range is the in-game slider's actual clamp, confirmed directly against
/// the live game (community guides disagree with each other and with this - not trusted here).
/// <c>client.lookatradius</c> has no enforced max, so its slider range instead comes from the real
/// values seen in <see cref="RustActionCatalog"/>'s own toggle example (0/0.2/10).
/// </summary>
internal static class ConvarEditorCatalog
{
    public static IReadOnlyList<ConvarEditorEntry> All { get; } =
    [
        new("graphics.fov", "ConvarFovLabel", ConvarEditorKind.Slider, Min: 70, Max: 90, Step: 1),
        new("audio.master", "ConvarMasterVolumeLabel", ConvarEditorKind.Slider, Min: 0, Max: 1, Step: 0.05),
        new("audio.instruments", "ConvarInstrumentsLabel", ConvarEditorKind.Toggle, ToggleOnValue: "1", ToggleOffValue: "0"),
        new("audio.voices", "ConvarVoicesLabel", ConvarEditorKind.Toggle, ToggleOnValue: "5", ToggleOffValue: "0"),
        new("client.lookatradius", "ConvarLookAtRadiusLabel", ConvarEditorKind.Slider, Min: 0, Max: 10, Step: 0.1),
        new("chat.enabled", "ConvarChatEnabledLabel", ConvarEditorKind.Toggle, ToggleOnValue: "True", ToggleOffValue: "False"),
        new("showtoast", "ConvarShowToastLabel", ConvarEditorKind.Text),
    ];
}