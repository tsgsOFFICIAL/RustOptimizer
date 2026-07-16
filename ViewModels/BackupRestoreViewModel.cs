using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Backup &amp; Restore page: a type selector switching between client.cfg ("Settings")
/// and keys.cfg ("Keybinds"), and the backup history for whichever type is selected - each entry
/// either a named manual snapshot or one auto-tagged before a preset apply or a restore. Restoring
/// or deleting an entry asks for confirmation first, since both are one-click-undoable-in-name-only:
/// a restore does take its own safety snapshot, but a delete is permanent.
/// </summary>
public sealed class BackupRestoreViewModel : ViewModelBase
{
    private readonly IConfigBackupService _configBackup;
    private readonly IDialogService _dialogs;
    private readonly SidebarViewModel _sidebar;

    private bool _isRustInstalled;
    private ConfigBackupType _selectedType = ConfigBackupType.Settings;
    private string _newBackupLabel = "";
    private string _statusText = "";
    private IReadOnlyList<ConfigBackupRow> _backups = [];

    /// <summary>Creates the view model and loads the backup history for the initially selected type.</summary>
    public BackupRestoreViewModel(ILocalizationService localization, IConfigBackupService configBackup,
        IRustProcessService rustProcess, SidebarViewModel sidebar, IDialogService dialogs)
        : base(localization)
    {
        _configBackup = configBackup;
        _dialogs = dialogs;
        _sidebar = sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;

        IsRustInstalled = rustProcess.GetInstallPath() != null;

        SelectTypeCommand = new RelayCommand<string>(tag =>
        {
            if (Enum.TryParse(tag, out ConfigBackupType type))
                SelectedType = type;
        });

        CreateBackupCommand = new RelayCommand(CreateBackup);
        RestoreCommand = new RelayCommand<string>(fileName => _ = RestoreAsync(fileName));
        DeleteCommand = new RelayCommand<string>(fileName => _ = DeleteAsync(fileName));

        RefreshBackups();
    }

    /// <summary>Switches <see cref="SelectedType"/> to the type named by its parameter.</summary>
    public RelayCommand<string> SelectTypeCommand { get; }

    /// <summary>Snapshots the selected type's live file under <see cref="NewBackupLabel"/>.</summary>
    public RelayCommand CreateBackupCommand { get; }

    /// <summary>Confirms, then restores the backup named by its parameter over the selected type's live file.</summary>
    public RelayCommand<string> RestoreCommand { get; }

    /// <summary>Confirms, then deletes the backup named by its parameter.</summary>
    public RelayCommand<string> DeleteCommand { get; }

    /// <summary>Whether Rust's install path could be resolved.</summary>
    public bool IsRustInstalled
    {
        get => _isRustInstalled;
        private set
        {
            if (SetProperty(ref _isRustInstalled, value))
                OnPropertyChanged(nameof(CanRestore));
        }
    }

    /// <summary>
    /// Whether a restore should be allowed - Rust has to be installed, and closed, since restoring
    /// while the game has the file open wouldn't stick.
    /// </summary>
    public bool CanRestore => IsRustInstalled && !_sidebar.IsRustRunning;

    /// <summary>Which cfg file's backup history is currently shown.</summary>
    public ConfigBackupType SelectedType
    {
        get => _selectedType;
        private set
        {
            if (SetProperty(ref _selectedType, value))
                RefreshBackups();
        }
    }

    /// <summary>The name to give the next manual backup, bound to the page's text input.</summary>
    public string NewBackupLabel
    {
        get => _newBackupLabel;
        set => SetProperty(ref _newBackupLabel, value);
    }

    /// <summary>Status message shown after creating, restoring, or deleting a backup.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>The backup history for <see cref="SelectedType"/>, newest first.</summary>
    public IReadOnlyList<ConfigBackupRow> Backups
    {
        get => _backups;
        private set => SetProperty(ref _backups, value);
    }

    /// <summary>Whether <see cref="Backups"/> has any entries, for the empty-state message.</summary>
    public bool HasBackups => _backups.Count > 0;

    /// <summary>Re-evaluates <see cref="CanRestore"/> whenever the sidebar's Rust-running state changes.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsRustRunning))
            OnPropertyChanged(nameof(CanRestore));
    }

    /// <summary>Creates a manual backup named <see cref="NewBackupLabel"/>, defaulting to a timestamp if left blank.</summary>
    private void CreateBackup()
    {
        string label = NewBackupLabel.Trim();
        if (label.Length == 0)
            label = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        bool success = _configBackup.CreateBackup(SelectedType, label);
        StatusText = Localization[success ? "BackupCreated" : "BackupCreateFailed"];

        if (success)
        {
            NewBackupLabel = "";
            RefreshBackups();
        }
    }

    /// <summary>Confirms, then restores the named backup over the selected type's live file.</summary>
    private async Task RestoreAsync(string? fileName)
    {
        if (fileName is null || !CanRestore)
            return;

        string label = Backups.FirstOrDefault(b => b.FileName == fileName)?.Label ?? fileName;
        bool confirmed = await _dialogs.ShowConfirmAsync(Localization, Localization["ConfirmRestoreTitle"],
            string.Format(Localization["ConfirmRestoreMessageFormat"], label), Localization["Restore"], isDestructive: false);

        if (!confirmed)
            return;

        bool success = _configBackup.Restore(SelectedType, fileName);
        StatusText = Localization[success ? "BackupRestored" : "BackupRestoreFailed"];
        RefreshBackups();
    }

    /// <summary>Confirms, then deletes the named backup.</summary>
    private async Task DeleteAsync(string? fileName)
    {
        if (fileName is null)
            return;

        string label = Backups.FirstOrDefault(b => b.FileName == fileName)?.Label ?? fileName;
        bool confirmed = await _dialogs.ShowConfirmAsync(Localization, Localization["ConfirmDeleteTitle"],
            string.Format(Localization["ConfirmDeleteMessageFormat"], label), Localization["Delete"], isDestructive: true);

        if (!confirmed)
            return;

        _configBackup.Delete(SelectedType, fileName);
        RefreshBackups();
    }

    /// <summary>
    /// Reloads <see cref="Backups"/> for the currently selected type. Call whenever the page
    /// becomes visible again - a backup created, restored, or removed while this view model sat
    /// cached (or a file added/removed from outside the app entirely) wouldn't otherwise show up.
    /// </summary>
    public void RefreshBackups()
    {
        Backups = _configBackup.GetBackups(SelectedType).Select(ToRow).ToList();
        OnPropertyChanged(nameof(HasBackups));
    }

    /// <summary>Converts a backup reading into its display row.</summary>
    private ConfigBackupRow ToRow(ConfigBackupInfo backup)
    {
        string label = backup.Label ?? Localization["BackupAutomaticLabel"];
        string tagText = Localization[backup.IsAutomatic ? "BackupAutomaticTag" : "BackupManualTag"];
        string created = backup.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return new ConfigBackupRow(backup.FileName, label, backup.IsAutomatic, tagText, created, FormatSize(backup.SizeBytes));
    }

    /// <summary>Formats a byte count as KB or MB, whichever reads better.</summary>
    private static string FormatSize(long bytes)
    {
        const double bytesPerKb = 1024.0;
        return bytes < bytesPerKb * bytesPerKb
            ? $"{bytes / bytesPerKb:0.#} KB"
            : $"{bytes / (bytesPerKb * bytesPerKb):0.#} MB";
    }
}