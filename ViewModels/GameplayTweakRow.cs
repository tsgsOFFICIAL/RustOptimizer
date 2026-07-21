using System.Collections.Generic;
using RustOptimizer.Interface;
using System.ComponentModel;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One row in the Gameplay page's recommended-tweaks list. <see cref="Label"/> and
/// <see cref="Description"/> are plain strings resolved once by <see cref="GameplayViewModel"/> -
/// a language switch rebuilds the whole row list rather than each row re-resolving its own text.
/// <see cref="IsEnabled"/> is the only mutable, bindable part: toggling it writes the tweak's
/// convar(s) to client.cfg immediately, reverting the toggle if the write fails.
/// </summary>
public sealed class GameplayTweakRow(string label, string description, GameplayTweak tweak, IConfigService configService, bool isEnabled) : INotifyPropertyChanged
{
    private bool _isEnabled = isEnabled;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The tweak's display name.</summary>
    public string Label { get; } = label;

    /// <summary>A short explanation of what the tweak does and why it's recommended.</summary>
    public string Description { get; } = description;

    /// <summary>
    /// Whether this tweak's convar(s) are currently set to their recommended values. Setting it
    /// writes client.cfg via <see cref="IConfigService.SetConvars"/>; if that fails (e.g. Rust is
    /// running), the toggle reverts to its previous state.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));

            Dictionary<string, string> convars = new(StringComparer.OrdinalIgnoreCase);
            foreach (ConvarValue convar in tweak.Convars)
                convars[convar.Convar] = value ? convar.EnabledValue : convar.DisabledValue;

            // No backup per toggle - these are small, individually reversible tweaks, not a whole
            // preset. Spamming a full client.cfg snapshot into the backup history for every flip
            // would just bury the backups that actually matter.
            if (configService.SetConvars(convars, createBackup: false))
                return;

            _isEnabled = !value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }
}