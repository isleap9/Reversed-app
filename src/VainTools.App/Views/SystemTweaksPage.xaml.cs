using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.App.Views;

public sealed partial class SystemTweaksPage : Page
{
    public ViewModels.SystemTweaksViewModel ViewModel { get; }

    public SystemTweaksPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.SystemTweaksViewModel>();
        this.DataContext = ViewModel;
    }
}