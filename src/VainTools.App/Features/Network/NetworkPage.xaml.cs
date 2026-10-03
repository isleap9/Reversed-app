using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Network;

public sealed partial class NetworkPage : Page
{
    public NetworkPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<NetworkPageViewModel>();
    }
}
