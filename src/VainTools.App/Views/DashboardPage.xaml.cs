using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.App.Views;

public sealed partial class DashboardPage : Page
{
    public ViewModels.DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.DashboardViewModel>();
        this.DataContext = ViewModel;
    }
}