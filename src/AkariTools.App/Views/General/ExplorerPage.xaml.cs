using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.General;

public sealed partial class ExplorerPage : Page
{
    public ExplorerViewModel? ViewModel => DataContext as ExplorerViewModel;

    public ExplorerPage(IRegistrySettingService registrySettingService)
    {
        this.InitializeComponent();
        DataContext = new ExplorerViewModel(registrySettingService);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.LoadAsync();
    }
}
