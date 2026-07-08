using RustOptimizer.ViewModels;
using Avalonia.Controls;

namespace RustOptimizer.Views;

/// <summary>
/// Renders the changelog Markdown into the content host. <see cref="MarkdownRenderer.Render"/>
/// produces a raw Avalonia control tree from a plain string, so this stays a code-behind concern
/// rather than something bindable directly.
/// </summary>
public partial class ChangelogView : UserControl
{
    public ChangelogView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ChangelogViewModel viewModel)
                ContentHost.Content = MarkdownRenderer.Render(viewModel.Markdown);
        };
    }
}