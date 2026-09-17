using System.Collections.Generic;
using Avalonia.Input;

namespace RustOptimizer.Service;

/// <summary>One recognized keys.cfg key token, paired with the plain-language name shown for it.</summary>
internal readonly record struct KnownKey(string Token, string DisplayName);

/// <summary>
/// The closed, enumerable set of key tokens keys.cfg recognizes (Rust's bind system is built on
/// Unity's standard <c>KeyCode</c> names, lowercased), plus the Avalonia input → token mapping the
/// key-capture dialog needs. Unlike <see cref="RustActionCatalog"/>, this carries no real
/// hallucination risk - it's a fixed, well-known keyboard/mouse vocabulary, not a guess at what
/// commands exist. <see cref="KnownKey.DisplayName"/>s are left untranslated, same convention
/// <c>DashboardViewModel.FormatBytes</c> already uses for unit symbols - a physical key's name reads
/// the same regardless of interface language.
/// </summary>
internal static class KeyTokenCatalog
{
    /// <summary>Every key token the "available keys" list offers, in keyboard-reading order.</summary>
    public static IReadOnlyList<KnownKey> AllKeys { get; } =
    [
        new("escape", "Esc"),
        new("f1", "F1"), new("f2", "F2"), new("f3", "F3"), new("f4", "F4"),
        new("f5", "F5"), new("f6", "F6"), new("f7", "F7"), new("f8", "F8"),
        new("f9", "F9"), new("f10", "F10"), new("f11", "F11"), new("f12", "F12"),

        new("backquote", "`"),
        new("1", "1"), new("2", "2"), new("3", "3"), new("4", "4"), new("5", "5"),
        new("6", "6"), new("7", "7"), new("8", "8"), new("9", "9"), new("0", "0"),
        new("minus", "-"), new("plus", "="), new("backspace", "Backspace"),

        new("tab", "Tab"),
        new("q", "Q"), new("w", "W"), new("e", "E"), new("r", "R"), new("t", "T"),
        new("y", "Y"), new("u", "U"), new("i", "I"), new("o", "O"), new("p", "P"),
        new("leftbracket", "["), new("rightbracket", "]"), new("backslash", "\\"),

        new("capslock", "Caps Lock"),
        new("a", "A"), new("s", "S"), new("d", "D"), new("f", "F"), new("g", "G"),
        new("h", "H"), new("j", "J"), new("k", "K"), new("l", "L"),
        new("semicolon", ";"), new("quote", "'"), new("enter", "Enter"),

        new("leftshift", "Left Shift"),
        new("z", "Z"), new("x", "X"), new("c", "C"), new("v", "V"), new("b", "B"),
        new("n", "N"), new("m", "M"), new("comma", ","), new("period", "."), new("slash", "/"),
        new("rightshift", "Right Shift"),

        new("leftctrl", "Left Ctrl"), new("leftcommand", "Win"), new("leftalt", "Left Alt"), new("space", "Space"),
        new("rightalt", "Right Alt"), new("menu", "Menu"), new("rightctrl", "Right Ctrl"),

        new("insert", "Insert"), new("delete", "Delete"), new("home", "Home"), new("end", "End"),
        new("pageup", "Page Up"), new("pagedown", "Page Down"), new("scrolllock", "Scroll Lock"), new("pause", "Pause"),

        new("uparrow", "Up Arrow"), new("downarrow", "Down Arrow"),
        new("leftarrow", "Left Arrow"), new("rightarrow", "Right Arrow"),

        new("numpad0", "Numpad 0"), new("numpad1", "Numpad 1"), new("numpad2", "Numpad 2"),
        new("numpad3", "Numpad 3"), new("numpad4", "Numpad 4"), new("numpad5", "Numpad 5"),
        new("numpad6", "Numpad 6"), new("numpad7", "Numpad 7"), new("numpad8", "Numpad 8"),
        new("numpad9", "Numpad 9"), new("numpadplus", "Numpad +"), new("numpadminus", "Numpad -"),
        new("numpadmultiply", "Numpad *"), new("numpaddivide", "Numpad /"),
        new("numpadperiod", "Numpad ."), new("numpadenter", "Numpad Enter"),

        new("mouse0", "Mouse 1 (Left)"), new("mouse1", "Mouse 2 (Right)"), new("mouse2", "Mouse 3 (Middle)"),
        new("mouse3", "Mouse 4"), new("mouse4", "Mouse 5"),
        new("mousewheelup", "Mouse Wheel Up"), new("mousewheeldown", "Mouse Wheel Down"),
    ];

    /// <summary>Every token usable as the modifier half of a <c>[modifier+key]</c> combo - the keyboard's own modifier keys.</summary>
    public static IReadOnlyList<string> ModifierTokens { get; } =
        ["leftshift", "rightshift", "leftctrl", "rightctrl", "leftalt", "rightalt"];

    /// <summary>Maps an Avalonia key (already known not to be a modifier) to its keys.cfg token, or <see langword="null"/> if unrecognized.</summary>
    public static string? FromAvaloniaKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString().ToLowerInvariant(),
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        >= Key.F1 and <= Key.F12 => "f" + ((int)key - (int)Key.F1 + 1),
        >= Key.NumPad0 and <= Key.NumPad9 => "numpad" + ((int)key - (int)Key.NumPad0),
        Key.Space => "space",
        Key.Enter => "enter",
        Key.Tab => "tab",
        Key.Escape => "escape",
        Key.Back => "backspace",
        Key.Delete => "delete",
        Key.Insert => "insert",
        Key.Home => "home",
        Key.End => "end",
        Key.PageUp => "pageup",
        Key.PageDown => "pagedown",
        Key.CapsLock => "capslock",
        Key.Up => "uparrow",
        Key.Down => "downarrow",
        Key.Left => "leftarrow",
        Key.Right => "rightarrow",
        Key.OemComma => "comma",
        Key.OemPeriod => "period",
        Key.OemSemicolon => "semicolon",
        Key.OemQuotes => "quote",
        Key.OemMinus => "minus",
        Key.OemPlus => "plus",
        Key.OemTilde => "backquote",
        Key.OemQuestion => "slash",
        Key.OemOpenBrackets => "leftbracket",
        Key.OemCloseBrackets => "rightbracket",
        Key.OemBackslash or Key.OemPipe => "backslash",
        Key.Add => "numpadplus",
        Key.Subtract => "numpadminus",
        Key.Multiply => "numpadmultiply",
        Key.Divide => "numpaddivide",
        Key.Decimal => "numpadperiod",
        Key.LWin or Key.RWin => "leftcommand",
        Key.Apps => "menu",
        Key.Scroll => "scrolllock",
        Key.Pause => "pause",
        // Numpad Enter reports as the same Key.Enter as the main Enter key on most keyboards/
        // drivers - Avalonia's Key enum has no reliable separate value for it, so it maps to
        // "enter" above like the main key; a minor imprecision, not a missing feature.
        _ => null
    };

    /// <summary>Maps an Avalonia modifier key to its keys.cfg modifier token, or <see langword="null"/> if it's not one of the four recognized modifiers.</summary>
    public static string? ModifierFromAvaloniaKey(Key key) => key switch
    {
        Key.LeftShift => "leftshift",
        Key.RightShift => "rightshift",
        Key.LeftCtrl => "leftctrl",
        Key.RightCtrl => "rightctrl",
        Key.LeftAlt => "leftalt",
        Key.RightAlt => "rightalt",
        _ => null
    };

    /// <summary>Maps a pointer press's <see cref="PointerUpdateKind"/> to its keys.cfg token, or <see langword="null"/> for anything that isn't a button press (e.g. plain movement).</summary>
    public static string? FromPointerUpdateKind(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.LeftButtonPressed => "mouse0",
        PointerUpdateKind.RightButtonPressed => "mouse1",
        PointerUpdateKind.MiddleButtonPressed => "mouse2",
        PointerUpdateKind.XButton1Pressed => "mouse3",
        PointerUpdateKind.XButton2Pressed => "mouse4",
        _ => null
    };

    /// <summary>The display name for a token, falling back to the token itself (uppercased) if it's not in <see cref="AllKeys"/> - e.g. a modifier token used standalone.</summary>
    public static string DisplayNameFor(string token)
    {
        foreach (KnownKey known in AllKeys)
        {
            if (known.Token == token)
                return known.DisplayName;
        }

        return ModifierDisplayName(token) ?? token.ToUpperInvariant();
    }

    /// <summary>Friendly names for the four modifier tokens when they appear as the modifier half of a combo.</summary>
    public static string? ModifierDisplayName(string token) => token switch
    {
        "leftshift" => "Left Shift",
        "rightshift" => "Right Shift",
        "leftctrl" => "Left Ctrl",
        "rightctrl" => "Right Ctrl",
        "leftalt" => "Left Alt",
        "rightalt" => "Right Alt",
        _ => null
    };
}