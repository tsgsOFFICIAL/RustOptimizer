using RustOptimizer.ViewModels;
using RustOptimizer.Interface;
using RustOptimizer.Service;
using Avalonia.Controls;

namespace RustOptimizer.Windows;

/// <summary>
/// A window shell hosting <see cref="Views.UpdateAvailableView"/>: title bar, offered-version
/// content, and Later/Update Now footer. Prompts the user that a newer version is available, with
/// the changelog rendered inline so they can see what changed without opening a separate window
/// before deciding to update.
/// </summary>
public partial class UpdateAvailableWindow : Window
{
    public UpdateAvailableWindow() : this(CreateDesignViewModel()) { }

    /// <summary>
    /// Creates an initialized view model for the Avalonia previewer.
    /// </summary>
    private static UpdateAvailableViewModel CreateDesignViewModel()
    {
        LocalizationService localization = new(new AppSettingsService());
        localization.Initialize();

        return new UpdateAvailableViewModel(localization, new UpdateService(),
            new UpdateInfo("0.0.0", "https://example.com", "RustOptimizer-0.0.0-self-contained.zip"),
            "# Sample for previewer");
    }

    /// <summary>
    /// Creates the update prompt for the given available version.
    /// </summary>
    /// <param name="localization">The localization service used to resolve UI strings.</param>
    /// <param name="updates">The update service used to apply the update if the user confirms.</param>
    /// <param name="update">The available update to offer.</param>
    /// <param name="changelog">The changes between the installed and available version, or empty if unavailable.</param>
    public UpdateAvailableWindow(ILocalizationService localization, IUpdateService updates, UpdateInfo update, string changelog)
        : this(new UpdateAvailableViewModel(localization, updates, update, changelog))
    {
    }

    public UpdateAvailableWindow(UpdateAvailableViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += Close;
    }
}