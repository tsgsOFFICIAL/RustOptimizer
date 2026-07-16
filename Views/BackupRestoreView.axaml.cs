using RustOptimizer.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace RustOptimizer.Views;

/// <summary>The Backup &amp; Restore page: a type selector and the backup history for whichever type is selected.</summary>
public partial class BackupRestoreView : UserControl
{
    /// <summary>Creates the view.</summary>
    public BackupRestoreView()
    {
        InitializeComponent();
    }

    /// <summary>Re-fetches the backup list, so changes made elsewhere (or to the files directly) show up on return.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as BackupRestoreViewModel)?.RefreshBackups();
    }
}