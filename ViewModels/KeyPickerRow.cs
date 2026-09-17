namespace RustOptimizer.ViewModels;

/// <summary>
/// One key on the visual keyboard/mouse picker: whether it's free, what's using it if not, whether
/// it matches the current search text, and a note when picking it as part of a combo wouldn't
/// replace a separate binding on its bare form (Rust fires both - see
/// <see cref="KeyCaptureDialogViewModel"/>'s doc comment).
/// </summary>
public sealed record KeyPickerRow(string Token, string DisplayName, bool IsFree, string UsedByText, bool IsMatch, string AlsoTriggersText, bool IsArmedModifier)
{
    /// <summary>Tooltip text combining <see cref="UsedByText"/> and <see cref="AlsoTriggersText"/>, or "" if neither applies.</summary>
    public string TooltipText => string.Join(" ", new[] { UsedByText, AlsoTriggersText }).Trim();
}