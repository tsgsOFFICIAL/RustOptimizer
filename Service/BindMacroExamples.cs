using System.Collections.Generic;
using RustOptimizer.Interface;

namespace RustOptimizer.Service;

/// <summary>One named example the manual macro builder can load as an editable starting point.</summary>
public readonly record struct BindMacroExample(string LabelKey, string DescriptionKey, IReadOnlyList<BindStage> Stages);

/// <summary>
/// Ready-made multi-stage macros the manual builder can load as a starting point. Every one of these
/// is copied stage-for-stage, verbatim, from a real, currently-working <c>~meta.exec</c> bind in this
/// app's ground-truth reference <c>keys.cfg</c> - the same file <see cref="RustActionCatalog"/> and
/// <see cref="KeybindsService"/> already treat as authoritative. They are deliberately kept separate
/// from <see cref="RustActionCatalog"/>'s browsable "Choose from list" catalog (see
/// <see cref="KeybindsService.GetKnownActions"/>) because a toggle macro like this is one player's own
/// personal setup, not a generic Rust action - it only ever shows up here, as something to copy and
/// edit, never suggested as if everyone binds it.
/// </summary>
internal static class BindMacroExamples
{
    public static IReadOnlyList<BindMacroExample> All { get; } =
    [
        new("ActionToggleAudioVolumeLabel", "ActionToggleAudioVolumeDesc",
        [
            new BindStage([new("audio.master", "audio.master 0.1"), new("showtoast", "showtoast 3 \"AUDIO LOW\"")]),
            new BindStage([new("audio.master", "audio.master 1"), new("showtoast", "showtoast 3 \"AUDIO MAX\"")]),
        ]),

        new("ActionCycleLookAtRadiusLabel", "ActionCycleLookAtRadiusDesc",
        [
            new BindStage([new("client.lookatradius", "client.lookatradius 10"), new("showtoast", "showtoast 3 \"LookAtRadius: MAX\"")]),
            new BindStage([new("client.lookatradius", "client.lookatradius 0"), new("showtoast", "showtoast 3 \"LookAtRadius: MIN\"")]),
            new BindStage([new("client.lookatradius", "client.lookatradius 0.2"), new("showtoast", "showtoast 3 \"LookAtRadius: DEFAULT\"")]),
        ]),

        new("ActionToggleFovLabel", "ActionToggleFovDesc",
        [
            new BindStage([new("graphics.fov", "graphics.fov 70"), new("showtoast", "showtoast 3 \"FOV: 70\"")]),
            new BindStage([new("graphics.fov", "graphics.fov 90"), new("showtoast", "showtoast 3 \"FOV: 90\"")]),
        ]),

        new("ActionInstrumentsOffLabel", "ActionInstrumentsOffDesc",
        [
            new BindStage([new("audio.instruments", "audio.instruments 0"), new("showtoast", "showtoast 3 \"INSTRUMENTS OFF\"")]),
        ]),

        new("ActionInstrumentsOnLabel", "ActionInstrumentsOnDesc",
        [
            new BindStage([new("audio.instruments", "audio.instruments 1"), new("showtoast", "showtoast 3 \"INSTRUMENTS ON\"")]),
        ]),

        new("ActionVoicesOnLabel", "ActionVoicesOnDesc",
        [
            new BindStage([new("audio.voices", "audio.voices 5"), new("showtoast", "showtoast 3 \"VOICES ON\"")]),
        ]),

        new("ActionVoicesOffLabel", "ActionVoicesOffDesc",
        [
            new BindStage([new("audio.voices", "audio.voices 0"), new("showtoast", "showtoast 3 \"VOICES OFF\"")]),
        ]),

        new("ActionToggleChatVisibilityLabel", "ActionToggleChatVisibilityDesc",
        [
            new BindStage([new(null, "chat.clear"), new("chat.enabled", "chat.enabled False"), new("showtoast", "showtoast 3 \"DISABLE CHAT\"")]),
            new BindStage([new("chat.enabled", "chat.enabled True"), new("showtoast", "showtoast 3 \"ENABLE CHAT\"")]),
        ]),
    ];
}