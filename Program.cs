using Microsoft.Extensions.DependencyInjection;
using RustOptimizer.Service.Logging;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia;
using System;

namespace RustOptimizer
{
    internal class Program
    {
        /// <summary>The app's DI container, built once at startup.</summary>
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>Builds the DI container and starts the Avalonia application.</summary>
        [STAThread]
        public static void Main(string[] args)
        {
            AppLog.Initialize();
            AppLog.RegisterGlobalExceptionHandlers();
            AppLog.Info("Program", "Application starting.");

            // Elevated re-launch for a single network tweak (see ElevationHelper) is handled before
            // the DI container/Avalonia lifetime are built at all, since this path never shows a window.
            if (args is ["--apply-network-tweak", string key, string value])
            {
#pragma warning disable CA1416 // This app only ever runs on Windows (see app.manifest); NetworkTweakElevationRunner is Windows-only.
                Environment.Exit(NetworkTweakElevationRunner.Run(key, value));
#pragma warning restore CA1416
                return;
            }

            try
            {
                // SystemInfoService/NetworkTweaksService are Windows-only; matches this app's WinExe/app.manifest-only deployment.
#pragma warning disable CA1416
                Services = new ServiceCollection()
                    .AddSingleton<IThemeService, ThemeService>()
                    .AddSingleton<ILocalizationService, LocalizationService>()
                    .AddSingleton<IUpdateService, UpdateService>()
                    .AddSingleton<IRustProcessService, RustProcessService>()
                    .AddSingleton<ISystemInfoService, SystemInfoService>()
                    .AddSingleton<ISystemTweaksService, SystemTweaksService>()
                    .AddSingleton<INetworkTweaksService, NetworkTweaksService>()
                    .AddSingleton<IDialogService, DialogService>()
                    .AddSingleton<IConfigBackupService, ConfigBackupService>()
                    .AddSingleton<IConfigService, ConfigService>()
                    .BuildServiceProvider();
#pragma warning restore CA1416

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                AppLog.Fatal("Program", "Unhandled exception during startup.", ex);
                throw;
            }
            finally
            {
                AppLog.Info("Program", "Application exiting.");
            }
        }

        /// <summary>Configures the Avalonia <see cref="AppBuilder"/> used to start the application.</summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}