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
    private bool _isRustInstalled = true;
    private bool _isLaunchButtonEnabled = true;
    private SidebarPage _activePage = SidebarPage.Dashboard;

    /// <summary>Creates the view model and resolves whether Rust is installed.</summary>
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
        IsRustInstalled = _rustProcess.GetInstallPath() != null;
    }

    /// <summary>
    /// Raised when the user selects a different page from the nav list.
    /// </summary>
    public event EventHandler<SidebarPage>? NavigationRequested;

    /// <summary>Launches Rust via Steam.</summary>
    public RelayCommand LaunchRustCommand { get; }

    /// <summary>Navigates to the page named by its parameter.</summary>
    public RelayCommand<string> NavigateCommand { get; }

    /// <summary>Whether the Rust process is currently running.</summary>
    public bool IsRustRunning
    {
        get => _isRustRunning;
        private set
        {
            if (SetProperty(ref _isRustRunning, value))
                OnPropertyChanged(nameof(RustStatusText));
        }
    }

    /// <summary>Whether Rust's install path could be resolved.</summary>
    public bool IsRustInstalled
    {
        get => _isRustInstalled;
        private set
        {
            if (SetProperty(ref _isRustInstalled, value))
            {
                OnPropertyChanged(nameof(RustStatusText));
                OnPropertyChanged(nameof(IsRustNotInstalled));
            }
        }
    }

    /// <summary>
    /// Inverse of <see cref="IsRustInstalled"/>, exposed for the sidebar's status dot styling
    /// (Avalonia <c>Classes.x</c> bindings need a direct bool, not a negation).
    /// </summary>
    public bool IsRustNotInstalled => !IsRustInstalled;

    /// <summary>Whether the "Launch Rust" button is enabled - false while installed-but-not-running is unconfirmed or Rust is already running.</summary>
    public bool IsLaunchButtonEnabled
    {
        get => _isLaunchButtonEnabled;
        private set => SetProperty(ref _isLaunchButtonEnabled, value);
    }

    /// <summary>The page currently selected in the nav list.</summary>
    public SidebarPage ActivePage
    {
        get => _activePage;
        private set => SetProperty(ref _activePage, value);
    }

    /// <summary>Localized status text reflecting whether Rust is installed, running, or neither.</summary>
    public string RustStatusText => Localization[!IsRustInstalled ? "RustNotInstalled" : IsRustRunning ? "RustRunning" : "RustNotRunning"];

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

    /// <summary>Refreshes <see cref="IsRustRunning"/> and <see cref="IsLaunchButtonEnabled"/>.</summary>
    private void Poll()
    {
        IsRustRunning = _rustProcess.IsRunning();
        IsLaunchButtonEnabled = IsRustInstalled && !IsRustRunning;
    }

    /// <summary>Launches Rust and disables the launch button until the next poll confirms it started.</summary>
    private void LaunchRust()
    {
        // Disable immediately so a slow Steam boot can't be spammed into multiple launches; the
        // next poll re-enables it on its own if Rust never actually starts (e.g. Steam missing).
        IsLaunchButtonEnabled = false;
        _rustProcess.Launch();
    }

    /// <summary>
    /// Programmatically selects a page, the same way clicking its nav button would - used by
    /// in-page links (e.g. the Dashboard's "More Details" row) that jump elsewhere without going
    /// through the nav rail itself.
    /// </summary>
    public void NavigateTo(SidebarPage page)
    {
        ActivePage = page;
        NavigationRequested?.Invoke(this, page);
    }

    /// <summary>Parses <paramref name="tag"/> as a <see cref="SidebarPage"/> and navigates to it.</summary>
    private void Navigate(string? tag)
    {
        if (Enum.TryParse(tag, out SidebarPage page))
            NavigateTo(page);
    }
}