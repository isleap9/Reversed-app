using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.General;

public sealed partial class VisualPage : Page
{
    public VisualViewModel? ViewModel => DataContext as VisualViewModel;

    public VisualPage(IRegistrySettingService registrySettingService)
    {
        this.InitializeComponent();
        DataContext = new VisualViewModel(registrySettingService);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.LoadAsync();
    }
}
