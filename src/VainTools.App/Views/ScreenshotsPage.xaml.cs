using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.App.Views;

public sealed partial class ScreenshotsPage : Page
{
    public ViewModels.ScreenshotsViewModel ViewModel { get; }

    public ScreenshotsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.ScreenshotsViewModel>();
        this.DataContext = ViewModel;
    }
}