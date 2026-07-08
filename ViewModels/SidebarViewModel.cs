using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using RustOptimizer.Controls;
using Avalonia.Threading;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the sidebar: current page selection, and the Rust running/launch state previously polled
/// directly by <c>MainWindow</c>.
/// </summary>
public sealed class SidebarViewModel : ViewModelBase
{
    private readonly IRustProcessService _rustProcess;
    private DispatcherTimer? _pollTimer;

    private bool _isRustRunning;
    private bool _isLaunchButtonEnabled = true;
    private SidebarPage _activePage = SidebarPage.Dashboard;

    public SidebarViewModel(ILocalizationService localization, IRustProcessService rustProcess)
        : base(localization)
    {
        _rustProcess = rustProcess;

        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is "Item" or null)
                OnPropertyChanged(nameof(RustStatusText));
        };

        LaunchRustCommand = new RelayCommand(LaunchRust);
        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    /// <summary>
    /// Raised when the user selects a different page from the nav list.
    /// </summary>
    public event EventHandler<SidebarPage>? NavigationRequested;

    public RelayCommand LaunchRustCommand { get; }
    public RelayCommand<string> NavigateCommand { get; }

    public bool IsRustRunning
    {
        get => _isRustRunning;
        private set
        {
            if (SetProperty(ref _isRustRunning, value))
                OnPropertyChanged(nameof(RustStatusText));
        }
    }

    public bool IsLaunchButtonEnabled
    {
        get => _isLaunchButtonEnabled;
        private set => SetProperty(ref _isLaunchButtonEnabled, value);
    }

    public SidebarPage ActivePage
    {
        get => _activePage;
        private set => SetProperty(ref _activePage, value);
    }

    public string RustStatusText => Localization[IsRustRunning ? "RustRunning" : "RustNotRunning"];

    /// <summary>
    /// Starts polling <see cref="IRustProcessService"/> every 3 seconds. Call from the view's
    /// attach-to-visual-tree lifecycle.
    /// </summary>
    public void StartPolling()
    {
        Poll();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();
    }

    /// <summary>
    /// Stops polling. Call from the view's detach-from-visual-tree lifecycle.
    /// </summary>
    public void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    private void Poll()
    {
        IsRustRunning = _rustProcess.IsRunning();
        IsLaunchButtonEnabled = !IsRustRunning;
    }

    private void LaunchRust()
    {
        // Disable immediately so a slow Steam boot can't be spammed into multiple launches; the
        // next poll re-enables it on its own if Rust never actually starts (e.g. Steam missing).
        IsLaunchButtonEnabled = false;
        _rustProcess.Launch();
    }

    private void Navigate(string? tag)
    {
        if (!Enum.TryParse(tag, out SidebarPage page))
            return;

        ActivePage = page;
        NavigationRequested?.Invoke(this, page);
    }
}