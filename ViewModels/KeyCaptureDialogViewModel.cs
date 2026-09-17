using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the key-capture dialog: a visual keyboard/mouse where every key shows whether it's free or
/// already bound - "show available keys" directly, rather than as a separate view - and picking one
/// (by clicking or by physically pressing it) finalizes the pick, or opens a conflict confirm if it's
/// already bound. Clicking or pressing one of the six modifier keys doesn't finalize - it arms that
/// modifier for a combo, exactly like actually holding it down would (tapping the same modifier again
/// clears it). The hosting window forwards raw input here via
/// <see cref="HandleModifierKey"/>/<see cref="HandleKeyToken"/> - this view model has no dependency
/// on Avalonia's input types, <see cref="KeyTokenCatalog"/> in the hosting window does that
/// translation. <see cref="CloseRequested"/> carries the chosen <see cref="KeyToken"/>, or
/// <see langword="null"/> on cancel.
/// <para>
/// Rust doesn't treat a <c>[modifier+key]</c> combo as replacing the bare key's own binding - both
/// fire together when the combo is pressed (confirmed on the official Facepunch wiki). Each row
/// carries an <see cref="KeyPickerRow.AlsoTriggersText"/> note for that case, shown in its tooltip,
/// rather than blocking anything - it's real Rust behavior, not a mistake to prevent.
/// </para>
/// </summary>
public sealed class KeyCaptureDialogViewModel : ViewModelBase
{
    private readonly IReadOnlyList<KeyBindingRow> _existingBindings;
    private readonly KeyToken? _excludeToken;
    private ModifierOption _selectedModifier;
    private bool _isConfirmingConflict;
    private string _conflictMessage = "";
    private KeyToken? _pendingToken;
    private string _searchText = "";

    /// <summary>
    /// Creates the dialog. <paramref name="excludeToken"/> is the token already being edited (if
    /// any), so re-picking the same token it already has isn't reported as a conflict with itself.
    /// </summary>
    public KeyCaptureDialogViewModel(ILocalizationService localization, IReadOnlyList<KeyBindingRow> existingBindings, KeyToken? excludeToken)
        : base(localization)
    {
        _existingBindings = existingBindings;
        _excludeToken = excludeToken;

        Modifiers =
        [
            new ModifierOption(null, Localization["KeyCaptureNoModifier"]),
            new ModifierOption("leftshift", "Left Shift"),
            new ModifierOption("rightshift", "Right Shift"),
            new ModifierOption("leftctrl", "Left Ctrl"),
            new ModifierOption("rightctrl", "Right Ctrl"),
            new ModifierOption("leftalt", "Left Alt"),
            new ModifierOption("rightalt", "Right Alt"),
        ];
        _selectedModifier = Modifiers[0];

        PressKeyCommand = new RelayCommand<string>(token =>
        {
            if (token is null)
                return;

            if (KeyTokenCatalog.ModifierTokens.Contains(token))
                HandleModifierKey(token);
            else
                SelectKey(token);
        });
        ConfirmConflictCommand = new RelayCommand(() =>
        {
            if (_pendingToken is { } token)
                CloseRequested?.Invoke(token);
        });
        CancelConflictCommand = new RelayCommand(() =>
        {
            IsConfirmingConflict = false;
            _pendingToken = null;
        });
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(null));
        ClearModifierCommand = new RelayCommand(() => SelectedModifier = Modifiers[0]);

        Rows = BuildRows();
    }

    /// <summary>Raised once a token is chosen and (if it conflicted) confirmed, or the user cancels.</summary>
    public event Action<KeyToken?>? CloseRequested;

    /// <summary>The seven modifier choices (None + the six modifier keys) - "None" is used to clear the armed modifier.</summary>
    public IReadOnlyList<ModifierOption> Modifiers { get; }

    /// <summary>The modifier currently armed for the combo being built, if any. Changing it re-evaluates every row's free/used status.</summary>
    public ModifierOption SelectedModifier
    {
        get => _selectedModifier;
        private set
        {
            if (SetProperty(ref _selectedModifier, value))
                Rows = BuildRows();
        }
    }

    /// <summary>Whether a modifier is currently armed - drives the "Shift is held" indicator and its clear button.</summary>
    public bool HasSelectedModifier => SelectedModifier.Token is not null;

    /// <summary>Text typed to filter the keyboard - matching keys stay full-strength, non-matching ones dim (see <see cref="KeyPickerRow.IsMatch"/>). Doesn't remove anything, since key positions are fixed on the visual keyboard.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                Rows = BuildRows();
        }
    }

    /// <summary>Every known key, with live free/used/match status for the currently selected modifier and search text.</summary>
    public IReadOnlyList<KeyPickerRow> Rows { get; private set; }

    /// <summary>Whether the chosen token is already bound and needs a second confirm before proceeding.</summary>
    public bool IsConfirmingConflict
    {
        get => _isConfirmingConflict;
        private set => SetProperty(ref _isConfirmingConflict, value);
    }

    /// <summary>"This is already bound to \<action\>." - only meaningful while <see cref="IsConfirmingConflict"/> is true.</summary>
    public string ConflictMessage
    {
        get => _conflictMessage;
        private set => SetProperty(ref _conflictMessage, value);
    }

    /// <summary>Presses a key on the visual keyboard/mouse - arms it as the modifier if it's one of the six modifier keys, otherwise picks it combined with <see cref="SelectedModifier"/>.</summary>
    public RelayCommand<string> PressKeyCommand { get; }

    /// <summary>Clears the currently armed modifier, going back to a bare key pick.</summary>
    public RelayCommand ClearModifierCommand { get; }

    /// <summary>Proceeds with the conflicting token anyway, reassigning it.</summary>
    public RelayCommand ConfirmConflictCommand { get; }

    /// <summary>Backs out of the conflict prompt without picking anything.</summary>
    public RelayCommand CancelConflictCommand { get; }

    /// <summary>Closes the dialog without picking anything.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>
    /// Called by the hosting window when a modifier key is physically pressed - arms
    /// <see cref="SelectedModifier"/> so the next non-modifier key/mouse press completes a combo,
    /// rather than finalizing on the modifier itself. Pressing the already-armed modifier again
    /// clears it, matching how releasing and re-pressing the same key would feel.
    /// </summary>
    public void HandleModifierKey(string modifierToken)
    {
        if (SelectedModifier.Token == modifierToken)
        {
            SelectedModifier = Modifiers[0];
            return;
        }

        ModifierOption? match = Modifiers.FirstOrDefault(m => m.Token == modifierToken);
        if (match is not null)
            SelectedModifier = match;
    }

    /// <summary>Called by the hosting window when a non-modifier key or mouse button/wheel is physically pressed - same effect as clicking that key.</summary>
    public void HandleKeyToken(string keyToken) => SelectKey(keyToken);

    /// <summary>Builds the token from the key plus whatever modifier is armed, and either finalizes it or asks for a conflict confirm.</summary>
    private void SelectKey(string key)
    {
        KeyToken token = new(SelectedModifier.Token, key);

        if (token == _excludeToken || FindConflict(token) is not { } conflictLabel)
        {
            CloseRequested?.Invoke(token);
            return;
        }

        _pendingToken = token;
        ConflictMessage = string.Format(Localization["KeyCaptureConflictFormat"], conflictLabel);
        IsConfirmingConflict = true;
    }

    /// <summary>The friendly (or raw) label of whatever currently binds <paramref name="token"/>, or <see langword="null"/> if it's free.</summary>
    private string? FindConflict(KeyToken token)
    {
        foreach (KeyBindingRow row in _existingBindings)
        {
            if (row.Token != token)
                continue;

            return row.ActionLabelKey is { } key ? Localization[key] : row.Command;
        }

        return null;
    }

    /// <summary>Rebuilds every row's free/used/match status for the currently armed modifier and search text.</summary>
    private List<KeyPickerRow> BuildRows() =>
        KeyTokenCatalog.AllKeys
            .Select(known =>
            {
                KeyToken token = new(SelectedModifier.Token, known.Token);
                string? usedBy = token == _excludeToken ? null : FindConflict(token);
                string usedByText = usedBy is { } label ? string.Format(Localization["KeyCaptureConflictFormat"], label) : "";

                // Only relevant while building a combo: does the bare key (no modifier) already
                // have its own, separate binding? Rust fires both when the combo is pressed.
                string alsoTriggersText = "";
                if (SelectedModifier.Token is not null && FindConflict(new KeyToken(null, known.Token)) is { } bareLabel)
                    alsoTriggersText = string.Format(Localization["KeyCaptureAlsoTriggersFormat"], bareLabel);

                bool isMatch = string.IsNullOrWhiteSpace(SearchText)
                    || known.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || known.Token.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                bool isArmedModifier = known.Token == SelectedModifier.Token;

                return new KeyPickerRow(known.Token, known.DisplayName, usedBy is null, usedByText, isMatch, alsoTriggersText, isArmedModifier);
            })
            .ToList();

    /// <summary>Looks up the live row for a specific token - lets the static keyboard/mouse XAML bind each hand-placed key button straight to its own row via an indexer binding (<c>{Binding [e]}</c>) instead of a generated <c>ItemsControl</c>.</summary>
    public KeyPickerRow this[string token] =>
        Rows.FirstOrDefault(r => r.Token == token) ?? new KeyPickerRow(token, KeyTokenCatalog.DisplayNameFor(token), true, "", true, "", false);
}

/// <summary>One entry in the modifier picker - <see cref="Token"/> is <see langword="null"/> for "no modifier."</summary>
public sealed record ModifierOption(string? Token, string DisplayName);