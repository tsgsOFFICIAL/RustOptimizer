using Microsoft.Extensions.DependencyInjection;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia;
using System;

namespace RustOptimizer
{
    internal class Program
    {
        public static IServiceProvider Services { get; private set; } = null!;

        [STAThread]
        public static void Main(string[] args)
        {
            Services = new ServiceCollection()
                .AddSingleton<IThemeService, ThemeService>()
                .AddSingleton<ILocalizationService, LocalizationService>()
                .BuildServiceProvider();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

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
