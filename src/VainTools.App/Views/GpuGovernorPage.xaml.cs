using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.App.Views;

public sealed partial class GpuGovernorPage : Page
{
    public ViewModels.GpuGovernorViewModel ViewModel { get; }

    public GpuGovernorPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.GpuGovernorViewModel>();
        this.DataContext = ViewModel;
    }
}