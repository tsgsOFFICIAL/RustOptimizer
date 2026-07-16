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

            try
            {
                // SystemInfoService is Windows-only; matches this app's WinExe/app.manifest-only deployment.
#pragma warning disable CA1416
                Services = new ServiceCollection()
                    .AddSingleton<IThemeService, ThemeService>()
                    .AddSingleton<ILocalizationService, LocalizationService>()
                    .AddSingleton<IUpdateService, UpdateService>()
                    .AddSingleton<IRustProcessService, RustProcessService>()
                    .AddSingleton<ISystemInfoService, SystemInfoService>()
                    .AddSingleton<ISystemTweaksService, SystemTweaksService>()
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