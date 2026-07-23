using System.Runtime.Versioning;
using RustOptimizer.ViewModels;
using RustOptimizer.Service;
using Avalonia.Controls;
using System;

namespace RustOptimizer
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        /// <summary>Creates the window with a design-time view model, for the Avalonia previewer.</summary>
        [SupportedOSPlatform("windows")]
        public MainWindow() : this(CreateDesignViewModel()) { }

        /// <summary>
        /// Creates a fully-wired view model for the Avalonia previewer. The update check and
        /// system-info poll never actually run there, since both are gated behind
        /// <see cref="Design.IsDesignMode"/> below / the view's own attach-to-visual-tree lifecycle.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static MainWindowViewModel CreateDesignViewModel()
        {
            AppSettingsService settings = new();
            settings.Initialize();

            ThemeService theme = new(settings);
            theme.Initialize();

            LocalizationService localization = new(settings);
            localization.Initialize();

            RustProcessService rustProcess = new();
            ConfigBackupService configBackup = new(rustProcess);
            return new MainWindowViewModel(theme, localization, new UpdateService(), rustProcess,
                new SystemInfoService(localization), new SystemTweaksService(rustProcess), new NetworkTweaksService(), new DialogService(),
                new ConfigService(rustProcess, configBackup), configBackup, new CleanupService(rustProcess), settings);
        }

        /// <summary>Creates the window bound to the given view model and wires up the startup update check.</summary>
        public MainWindow(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            if (!Design.IsDesignMode)
                Opened += OnMainWindowOpened;
        }

        /// <summary>
        /// Kicks off the startup update check once the window is shown. Runs once per launch.
        /// </summary>
        private async void OnMainWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnMainWindowOpened;
            await _viewModel.CheckForUpdatesOnStartupAsync();
        }
    }
}