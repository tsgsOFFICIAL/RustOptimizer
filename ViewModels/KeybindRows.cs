using RustOptimizer.Interface;
using System.Collections.Generic;

namespace RustOptimizer.ViewModels;

/// <summary>One bind line, ready for display: its friendly (or raw) action text, its key/combo badge text, and its category as a small muted tag.</summary>
public sealed record KeybindRow(KeyBindingRow Binding, string ActionText, string KeyText, string CategoryLabel);

/// <summary>One collapsible category section in the Keybinds page's browsing view (search bypasses this and shows a flat list instead).</summary>
public sealed record KeybindCategoryGroup(string CategoryKey, string CategoryLabel, IReadOnlyList<KeybindRow> Rows, bool IsExpanded);

/// <summary>One row in the bind catalog dialog's list - a known action plus its live search-match state.</summary>
public sealed record CatalogActionRow(RustAction Action, string DisplayName, string CategoryLabel, string DescriptionText, bool IsMatch);

/// <summary>One entry in the manual macro builder's per-line convar picker - a curated convar, or the "Custom command" sentinel (<see cref="Entry"/> null) for free text.</summary>
public sealed record ConvarPickerOption(Service.ConvarEditorEntry? Entry, string DisplayText);

/// <summary>One loadable example in the manual macro builder's "Load Example" picker.</summary>
public sealed record ExampleOption(Service.BindMacroExample Example, string DisplayText, string DescriptionText);