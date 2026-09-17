using System.Collections.Generic;
using RustOptimizer.Interface;

namespace RustOptimizer.Service;

/// <summary>
/// Every action this app can put a friendly name to, one entry per distinct command string (several
/// keys share a command, e.g. Tab and I both bind <c>inventory.toggle</c> - that's one catalog entry
/// used by two <see cref="KeyBindingRow"/>s, not two entries). The plain, single-purpose commands are
/// verified straight from Rust's own shipped <c>cfg/keys_default.cfg</c> (including the multi-command
/// combos it builds when the same key gets more than one <c>bind_default</c> line, e.g. <c>e</c>
/// becoming <c>+use;+nextskin</c>). The <c>~meta.exec "A" "B"</c> toggle-cycle entries are different -
/// they're not Rust defaults at all, they're one real player's own custom macros, copied verbatim from
/// this app's ground-truth reference <c>keys.cfg</c> so they're still recognized and named nicely if a
/// user's own bindings happen to match them. That distinction matters:
/// <see cref="KeybindsService.GetKnownActions"/> deliberately excludes every "~"-prefixed entry from
/// what "Choose from list" offers - a personal macro isn't a generic action to suggest to everyone, so
/// it only appears there as a loadable example (<see cref="BindMacroExamples"/>), never as a catalog
/// pick. <see cref="KeybindsService"/> matches a bind line's command against <see cref="RustAction.Command"/>
/// by exact string equality, so a single wrong character here just means that one action shows up
/// unmatched under "Other" rather than under its real name; nothing breaks.
/// </summary>
internal static class RustActionCatalog
{
    public static IReadOnlyList<RustAction> All { get; } =
    [
        // Movement
        new("+forward", "ActionMoveForwardLabel", "KeybindCategoryMovement", "ActionMoveForwardDesc"),
        new("+backward", "ActionMoveBackwardLabel", "KeybindCategoryMovement", "ActionMoveBackwardDesc"),
        new("+left", "ActionMoveLeftLabel", "KeybindCategoryMovement", "ActionMoveLeftDesc"),
        new("+right", "ActionMoveRightLabel", "KeybindCategoryMovement", "ActionMoveRightDesc"),
        new("+jump", "ActionJumpLabel", "KeybindCategoryMovement", "ActionJumpDesc"),
        new("duck", "ActionCrouchToggleLabel", "KeybindCategoryMovement", "ActionCrouchToggleDesc"),
        new("+duck", "ActionCrouchHoldLabel", "KeybindCategoryMovement", "ActionCrouchHoldDesc"),
        new("+sprint;+snapping", "ActionSprintLabel", "KeybindCategoryMovement", "ActionSprintDesc"),
        new("+autowalk", "ActionAutoWalkLabel", "KeybindCategoryMovement", "ActionAutoWalkDesc"),
        new("""showtoast 3 "AUTO SWIM";buttons.forward;buttons.jump""", "ActionAutoSwimLabel", "KeybindCategoryMovement", "ActionAutoSwimDesc"),

        // Combat
        new("attack", "ActionAttackTapLabel", "KeybindCategoryCombat", "ActionAttackTapDesc"),
        new("+attack", "ActionAttackHoldLabel", "KeybindCategoryCombat", "ActionAttackHoldDesc"),
        new("+attack2", "ActionAimAttackLabel", "KeybindCategoryCombat", "ActionAimAttackDesc"),
        new("+reload", "ActionReloadLabel", "KeybindCategoryCombat", "ActionReloadDesc"),
        new("+firemode", "ActionCycleFireModeLabel", "KeybindCategoryCombat", "ActionCycleFireModeDesc"),

        // Interaction
        new("+use;+nextskin", "ActionUseNextSkinLabel", "KeybindCategoryInteraction", "ActionUseNextSkinDesc"),
        new("+focusmap;lighttoggle", "ActionFocusMapToggleLightLabel", "KeybindCategoryInteraction", "ActionFocusMapToggleLightDesc"),
        new("+prevskin;+dropitemsingle", "ActionPrevSkinDropItemLabel", "KeybindCategoryInteraction", "ActionPrevSkinDropItemDesc"),
        new("examineheld", "ActionExamineHeldLabel", "KeybindCategoryInteraction", "ActionExamineHeldDesc"),
        new("+hoverloot", "ActionQuickLootLabel", "KeybindCategoryInteraction", "ActionQuickLootDesc"),
        new("+dropitemstack", "ActionDropItemStackLabel", "KeybindCategoryInteraction", "ActionDropItemStackDesc"),
        new("+directionaldrop", "ActionDirectionalDropLabel", "KeybindCategoryInteraction", "ActionDirectionalDropDesc"),

        // Inventory
        new("inventory.toggle", "ActionToggleInventoryLabel", "KeybindCategoryInventory", "ActionToggleInventoryDesc"),
        new("inventory.togglecrafting", "ActionToggleCraftingLabel", "KeybindCategoryInventory", "ActionToggleCraftingDesc"),
        new("+slot1", "ActionSelectSlot1Label", "KeybindCategoryInventory", "ActionSelectSlot1Desc"),
        new("+slot2", "ActionSelectSlot2Label", "KeybindCategoryInventory", "ActionSelectSlot2Desc"),
        new("+slot3", "ActionSelectSlot3Label", "KeybindCategoryInventory", "ActionSelectSlot3Desc"),
        new("+slot4", "ActionSelectSlot4Label", "KeybindCategoryInventory", "ActionSelectSlot4Desc"),
        new("+slot5", "ActionSelectSlot5Label", "KeybindCategoryInventory", "ActionSelectSlot5Desc"),
        new("+slot6", "ActionSelectSlot6Label", "KeybindCategoryInventory", "ActionSelectSlot6Desc"),
        new("+holsteritem", "ActionHolsterItemLabel", "KeybindCategoryInventory", "ActionHolsterItemDesc"),
        new("+invprev", "ActionPrevInventoryItemLabel", "KeybindCategoryInventory", "ActionPrevInventoryItemDesc"),
        new("+invnext", "ActionNextInventoryItemLabel", "KeybindCategoryInventory", "ActionNextInventoryItemDesc"),

        // Communication
        new("chat.open", "ActionOpenChatLabel", "KeybindCategoryCommunication", "ActionOpenChatDesc"),
        new("clan.toggleclan", "ActionToggleClanMenuLabel", "KeybindCategoryCommunication", "ActionToggleClanMenuDesc"),
        new("+gestures", "ActionOpenGestureWheelLabel", "KeybindCategoryCommunication", "ActionOpenGestureWheelDesc"),
        new("+ping", "ActionPingLabel", "KeybindCategoryCommunication", "ActionPingDesc"),
        new("+voice", "ActionVoiceChatLabel", "KeybindCategoryCommunication", "ActionVoiceChatDesc"),
        new("""
            ~meta.exec "chat.clear" "chat.enabled False" "showtoast 3 \"DISABLE CHAT\"";meta.exec "chat.enabled True" "showtoast 3 \"ENABLE CHAT\""
            """, "ActionToggleChatVisibilityLabel", "KeybindCategoryCommunication", "ActionToggleChatVisibilityDesc"),

        // Camera & Map
        new("+markcurrentpos", "ActionMarkPositionLabel", "KeybindCategoryCameraMap", "ActionMarkPositionDesc"),
        new("+map;+focusmap", "ActionOpenMapLabel", "KeybindCategoryCameraMap", "ActionOpenMapDesc"),
        new("+altlook;+headlerp 4;headlerp 0", "ActionAltLookLabel", "KeybindCategoryCameraMap", "ActionAltLookDesc"),
        new("+zoomincrease", "ActionZoomInLabel", "KeybindCategoryCameraMap", "ActionZoomInDesc"),
        new("+zoomdecrease", "ActionZoomOutLabel", "KeybindCategoryCameraMap", "ActionZoomOutDesc"),

        // Building & Vehicles
        new("swapseats", "ActionSwapSeatsLabel", "KeybindCategoryBuildingVehicles", "ActionSwapSeatsDesc"),
        new("swaptoseat 0", "ActionSwapToSeat1Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat1Desc"),
        new("swaptoseat 1", "ActionSwapToSeat2Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat2Desc"),
        new("swaptoseat 2", "ActionSwapToSeat3Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat3Desc"),
        new("swaptoseat 3", "ActionSwapToSeat4Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat4Desc"),
        new("swaptoseat 4", "ActionSwapToSeat5Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat5Desc"),
        new("swaptoseat 5", "ActionSwapToSeat6Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat6Desc"),
        new("swaptoseat 6", "ActionSwapToSeat7Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat7Desc"),
        new("swaptoseat 7", "ActionSwapToSeat8Label", "KeybindCategoryBuildingVehicles", "ActionSwapToSeat8Desc"),
        new("+wireslackup", "ActionWireSlackUpLabel", "KeybindCategoryBuildingVehicles", "ActionWireSlackUpDesc"),
        new("+wireslackdown", "ActionWireSlackDownLabel", "KeybindCategoryBuildingVehicles", "ActionWireSlackDownDesc"),

        // Audio & Toggles
        new("""
            ~meta.exec "audio.master 0.1" "showtoast 3 \"AUDIO LOW\"";meta.exec "audio.master 1" "showtoast 3 \"AUDIO MAX\""
            """, "ActionToggleAudioVolumeLabel", "KeybindCategoryAudioToggles", "ActionToggleAudioVolumeDesc"),
        new("""
            ~meta.exec "client.lookatradius 10" "showtoast 3 \"LookAtRadius: MAX\"";meta.exec "client.lookatradius 0" "showtoast 3 \"LookAtRadius: MIN\"";meta.exec "client.lookatradius 0.2" "showtoast 3 \"LookAtRadius: DEFAULT\""
            """, "ActionCycleLookAtRadiusLabel", "KeybindCategoryAudioToggles", "ActionCycleLookAtRadiusDesc"),
        new("""
            ~meta.exec "graphics.fov 70" "showtoast 3 \"FOV: 70\"";meta.exec "graphics.fov 90" "showtoast 3 \"FOV: 90\""
            """, "ActionToggleFovLabel", "KeybindCategoryAudioToggles", "ActionToggleFovDesc"),
        new("""
            ~meta.exec "audio.instruments 0" "showtoast 3 \"INSTRUMENTS OFF\""
            """, "ActionInstrumentsOffLabel", "KeybindCategoryAudioToggles", "ActionInstrumentsOffDesc"),
        new("""
            ~meta.exec "audio.instruments 1" "showtoast 3 \"INSTRUMENTS ON\""
            """, "ActionInstrumentsOnLabel", "KeybindCategoryAudioToggles", "ActionInstrumentsOnDesc"),
        new("""
            ~meta.exec "audio.voices 5" "showtoast 3 \"VOICES ON\""
            """, "ActionVoicesOnLabel", "KeybindCategoryAudioToggles", "ActionVoicesOnDesc"),
        new("""
            ~meta.exec "audio.voices 0" "showtoast 3 \"VOICES OFF\""
            """, "ActionVoicesOffLabel", "KeybindCategoryAudioToggles", "ActionVoicesOffDesc"),

        // Music (instrument playing)
        new("+noteoctaveupmod", "ActionNoteOctaveUpLabel", "KeybindCategoryMusic", "ActionNoteOctaveUpDesc"),
        new("+notesharpmod", "ActionNoteSharpLabel", "KeybindCategoryMusic", "ActionNoteSharpDesc"),
        new("+notea", "ActionPlayNoteALabel", "KeybindCategoryMusic", "ActionPlayNoteADesc"),
        new("+noteb", "ActionPlayNoteBLabel", "KeybindCategoryMusic", "ActionPlayNoteBDesc"),

        // Console / Debug
        new("consoletoggle;combatlog 100", "ActionToggleConsoleLabel", "KeybindCategoryConsole", "ActionToggleConsoleDesc"),
        new("legacyconsoletoggle;combatlog 100", "ActionToggleLegacyConsoleLabel", "KeybindCategoryConsole", "ActionToggleLegacyConsoleDesc"),

        // Other
        new("no_input", "ActionNoInputLabel", "KeybindCategoryOther", "ActionNoInputDesc"),
        new("kill", "ActionKillSelfLabel", "KeybindCategoryOther", "ActionKillSelfDesc"),
    ];
}