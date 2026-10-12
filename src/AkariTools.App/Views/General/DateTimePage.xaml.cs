using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.General;

public sealed partial class DateTimePage : Page
{
    public DateTimeViewModel? ViewModel => DataContext as DateTimeViewModel;

    public DateTimePage(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl)
    {
        this.InitializeComponent();
        DataContext = new DateTimeViewModel(registrySettingService, serviceControl);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.LoadAsync();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null && sender is Button { Tag: string server })
            ViewModel.RemoveServerCommand.Execute(server);
    }
}
