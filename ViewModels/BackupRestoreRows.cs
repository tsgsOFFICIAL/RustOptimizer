namespace RustOptimizer.ViewModels;

/// <summary>
/// One stored backup, formatted for display as a card in the Backup &amp; Restore page's list.
/// <see cref="Label"/> is either the manual name it was given or a localized "Automatic" fallback.
/// <see cref="IsAutomatic"/> drives the card's Automatic/Manual tag styling; <see cref="TagText"/>
/// is that same distinction as localized display text.
/// </summary>
public sealed record ConfigBackupRow(string FileName, string Label, bool IsAutomatic, string TagText, string CreatedText, string SizeText);