using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the "update available" prompt: the offered version, its changelog, and applying or
/// dismissing the update. <see cref="CloseRequested"/> lets the hosting
/// <c>UpdateAvailableWindow</c> close itself without this view model referencing <c>Window</c>
/// directly.
/// </summary>
public sealed class UpdateAvailableViewModel : ViewModelBase
{
    private readonly IUpdateService _updates;
    private readonly UpdateInfo _update;

    private string _statusText = "";
    private bool _isStatusVisible;
    private bool _isBusy;

    public UpdateAvailableViewModel(ILocalizationService localization, IUpdateService updates, UpdateInfo update, string changelog)
        : base(localization)
    {
        _updates = updates;
        _update = update;

        Version = update.Version;
        Changelog = changelog;
        HasChangelog = changelog.Length > 0;

        LaterCommand = new RelayCommand(() => CloseRequested?.Invoke(), () => !IsBusy);
        UpdateNowCommand = new AsyncRelayCommand(UpdateNowAsync);
    }

    public event Action? CloseRequested;

    public string Version { get; }
    public string Changelog { get; }
    public bool HasChangelog { get; }

    public RelayCommand LaterCommand { get; }
    public AsyncRelayCommand UpdateNowCommand { get; }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public bool IsStatusVisible { get => _isStatusVisible; private set => SetProperty(ref _isStatusVisible, value); }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                LaterCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task UpdateNowAsync()
    {
        IsBusy = true;
        StatusText = Localization["Updating"];
        IsStatusVisible = true;

        try
        {
            // On success this exits the process directly, so nothing after this call runs on the happy path.
            await _updates.ApplyUpdateAsync(_update);
        }
        catch (Exception ex)
        {
            StatusText = $"{Localization["UpdateFailed"]} {ex.Message}";
            IsBusy = false;
        }
    }
}