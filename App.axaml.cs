using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia;
using System;

namespace RustOptimizer
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (Design.IsDesignMode)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }

            IServiceProvider services = Program.Services
                ?? throw new InvalidOperationException("ServiceProvider not initialized");

            IThemeService theme = services.GetRequiredService<IThemeService>();
            ILocalizationService localization = services.GetRequiredService<ILocalizationService>();
            IUpdateService updates = services.GetRequiredService<IUpdateService>();
            IRustProcessService rustProcess = services.GetRequiredService<IRustProcessService>();
            ISystemInfoService systemInfo = services.GetRequiredService<ISystemInfoService>();
            IDialogService dialogs = services.GetRequiredService<IDialogService>();
            IConfigService configService = services.GetRequiredService<IConfigService>();

            theme.Initialize();
            localization.Initialize();

            MainWindowViewModel viewModel = new(theme, localization, updates, rustProcess, systemInfo, dialogs, configService);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow(viewModel);

            base.OnFrameworkInitializationCompleted();
        }
    }
}