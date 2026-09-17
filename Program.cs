using Microsoft.Extensions.DependencyInjection;
using RustOptimizer.Service.Logging;
using System.Collections.Generic;
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

            // Elevated re-launches (see ElevationHelper) are handled before the DI container/Avalonia
            // lifetime are built at all, since neither path ever shows a window.
#pragma warning disable CA1416 // This app only ever runs on Windows (see app.manifest); both elevated runners are Windows-only.
            if (args is ["--apply-network-tweak", _, _] or ["--apply-network-tweaks", ..] or [CleanupElevationRunner.Argument])
            {
                int exitCode;
                try
                {
                    exitCode = args[0] switch
                    {
                        CleanupElevationRunner.Argument => CleanupElevationRunner.Run(),
                        "--apply-network-tweaks" => NetworkTweakElevationRunner.RunBatch(ParseNetworkTweakPairs(args)),
                        _ => NetworkTweakElevationRunner.Run(args[1], args[2])
                    };
                }
                catch (Exception ex)
                {
                    AppLog.Fatal("Program", "Unhandled exception in an elevated helper.", ex);
                    exitCode = 1;
                }

                // Environment.Exit terminates the process immediately without reliably running a
                // finally block, so "Application exiting" is logged explicitly here rather than
                // relying on the main path's try/finally below - every launch of this exe, elevated
                // helper or full UI, should leave a symmetric start/exit pair in the log.
                AppLog.Info("Program", "Application exiting.");
                Environment.Exit(exitCode);
            }
#pragma warning restore CA1416

            try
            {
                // SystemInfoService/NetworkTweaksService are Windows-only; matches this app's WinExe/app.manifest-only deployment.
#pragma warning disable CA1416
                Services = new ServiceCollection()
                    .AddSingleton<IAppSettingsService, AppSettingsService>()
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
                    .AddSingleton<ICleanupService, CleanupService>()
                    .AddSingleton<ISmartOptimizationService, SmartOptimizationService>()
                    .AddSingleton<IKeybindsService, KeybindsService>()
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

        /// <summary>
        /// Pairs up "--apply-network-tweaks key1 value1 key2 value2 ..." into (key, value) tuples for
        /// <see cref="NetworkTweakElevationRunner.RunBatch"/>. A trailing unpaired key (a malformed
        /// re-launch - this app only ever constructs these args itself) is simply dropped.
        /// </summary>
        private static IReadOnlyList<(string Key, string Value)> ParseNetworkTweakPairs(string[] args)
        {
            List<(string, string)> pairs = [];
            for (int i = 1; i + 1 < args.Length; i += 2)
                pairs.Add((args[i], args[i + 1]));

            return pairs;
        }
    }
}