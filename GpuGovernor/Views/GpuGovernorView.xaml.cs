using Microsoft.UI.Xaml.Controls;
using VainTools.GpuGovernor.ViewModels;
using VainTools.Services;

namespace VainTools.GpuGovernor.Views;

/// <summary>
/// Code-behind for GpuGovernorView.
/// Sets up the DataContext with the ViewModel.
/// </summary>
public sealed partial class GpuGovernorView : Page
{
    /// <summary>
    /// The ViewModel instance for this view.
    /// </summary>
    public GpuGovernorViewModel ViewModel { get; private set; }

    public GpuGovernorView()
    {
        this.InitializeComponent();

        // Create the ViewModel with the GPU service
        // In a production app, this would be injected via DI
        var gpuService = new GpuService();
        ViewModel = new GpuGovernorViewModel(gpuService);

        // Set the DataContext for data binding
        this.DataContext = ViewModel;
    }
}
