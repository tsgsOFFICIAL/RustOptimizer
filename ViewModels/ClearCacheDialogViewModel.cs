using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Threading;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the Clear Cache prompt, and owns the cleanup run itself. Everything the cleanup touches is
/// fire-and-forget except the three options here, so all three default to <see langword="true"/> and
/// exist purely to let the user opt out.
/// <para>
/// The prompt stays open while cleaning, swapping its options for a progress bar - a run can take a
/// while on a machine that hasn't been cleaned in months, and closing to a silent frozen button was
/// worse than showing what it's working on. <see cref="CloseRequested"/> carries the finished
/// outcome, or <see langword="null"/> if the user cancelled before starting.
/// </para>
/// </summary>
public sealed class ClearCacheDialogViewModel : ViewModelBase
{
    private readonly ICleanupService _cleanup;
    private readonly CancellationTokenSource _cancellation = new();

    private bool _emptyRecycleBin = true;
    private bool _clearThumbnailCache = true;
    private bool _includeSystemFiles = true;
    private bool _isRunning;
    private string _progressLabel = "";
    private int _progressValue;
    private int _progressMaximum = 1;

    /// <summary>Creates the view model with every option enabled and nothing running yet.</summary>
    public ClearCacheDialogViewModel(ILocalizationService localization, ICleanupService cleanup) : base(localization)
    {
        _cleanup = cleanup;

        ConfirmCommand = new RelayCommand(() => _ = RunAsync());
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>Raised when the prompt should close, carrying the run's outcome or <see langword="null"/> if nothing ran.</summary>
    public event Action<CleanupOutcome?>? CloseRequested;

    /// <summary>Whether the Recycle Bin should be emptied. The only option that destroys recoverable files.</summary>
    public bool EmptyRecycleBin
    {
        get => _emptyRecycleBin;
        set => SetProperty(ref _emptyRecycleBin, value);
    }

    /// <summary>Whether the thumbnail cache should be cleared, which restarts Explorer.</summary>
    public bool ClearThumbnailCache
    {
        get => _clearThumbnailCache;
        set => SetProperty(ref _clearThumbnailCache, value);
    }

    /// <summary>Whether the admin-only targets are included, which raises a UAC prompt.</summary>
    public bool IncludeSystemFiles
    {
        get => _includeSystemFiles;
        set => SetProperty(ref _includeSystemFiles, value);
    }

    /// <summary>
    /// Whether a cleanup is in progress. Drives the swap between the options view and the progress
    /// view, since the options can't be changed once the run has started.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                OnPropertyChanged(nameof(IsConfiguring));
        }
    }

    /// <summary>Whether the options are still being chosen - the inverse of <see cref="IsRunning"/>.</summary>
    public bool IsConfiguring => !IsRunning;

    /// <summary>The localized name of the group currently being cleared, e.g. "Shader caches".</summary>
    public string ProgressLabel
    {
        get => _progressLabel;
        private set => SetProperty(ref _progressLabel, value);
    }

    /// <summary>How many groups have finished, bound to the progress bar's value.</summary>
    public int ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    /// <summary>How many groups this run will process, bound to the progress bar's maximum.</summary>
    public int ProgressMaximum
    {
        get => _progressMaximum;
        private set => SetProperty(ref _progressMaximum, value);
    }

    /// <summary>Starts the cleanup with the selected options.</summary>
    public RelayCommand ConfirmCommand { get; }

    /// <summary>Closes the prompt before starting, or stops a run already in progress.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>
    /// Runs the cleanup, reporting progress as each group starts, then closes the prompt with the
    /// outcome. <see cref="Progress{T}"/> is constructed here, on the UI thread, so its callbacks
    /// marshal back automatically rather than mutating bound properties from a worker.
    /// </summary>
    private async System.Threading.Tasks.Task RunAsync()
    {
        if (IsRunning)
            return;

        IsRunning = true;

        Progress<CleanupProgress> progress = new(update =>
        {
            ProgressMaximum = Math.Max(update.TotalSteps, 1);
            ProgressValue = update.CompletedSteps;
            ProgressLabel = update.LabelKey.Length > 0 ? Localization[update.LabelKey] : "";
        });

        CleanupOptions options = new(EmptyRecycleBin, ClearThumbnailCache, IncludeSystemFiles);
        CleanupOutcome outcome = await _cleanup.CleanAsync(options, progress, _cancellation.Token);

        CloseRequested?.Invoke(outcome);
    }

    /// <summary>
    /// Cancels a run in progress, or closes the prompt outright if nothing has started. A cancelled
    /// run still closes through <see cref="RunAsync"/> with whatever it managed to free.
    /// </summary>
    private void Cancel()
    {
        if (IsRunning)
            _cancellation.Cancel();
        else
            CloseRequested?.Invoke(null);
    }
}
