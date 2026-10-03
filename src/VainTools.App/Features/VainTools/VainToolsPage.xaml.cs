using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace VainTools.App.Features.VainTools;

/// <summary>
/// Vain Toolbox's own settings, including <c>.vain</c> profile import and
/// the restore-defaults flow.
/// </summary>
public sealed partial class VainToolsPage : Page
{
    public VainToolsViewModel ViewModel { get; }

    public VainToolsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<VainToolsViewModel>();
    }

    private void OnDropZoneDragOver(object sender, DragEventArgs e)
    {
        // Only accept a single .vain file; anything else shows the "no" cursor.
        if (e.DataView.Contains(StandardDataFormats.StorageItems) && HasSingleVainFile(e.DataView))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Import profile";
            e.DragUIOverride.IsCaptionVisible = true;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.None;
    }

    private void OnDropZoneDragLeave(object sender, DragEventArgs e)
    {
        // Reset the visual affordance; nothing to clean up beyond the border.
        DropZone.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
    }

    private async void OnDropZoneDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var file = items.OfType<Windows.Storage.StorageFile>().FirstOrDefault();

            if (file is null || !file.Name.EndsWith(".vain", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await ViewModel.ImportFromPathAsync(file.Path);
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private static bool HasSingleVainFile(DataPackageView view)
        => view.AvailableFormats.Contains(StandardDataFormats.StorageItems);
}
