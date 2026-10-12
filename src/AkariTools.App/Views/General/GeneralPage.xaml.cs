using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.General;

public sealed partial class GeneralPage : Page
{
    public GeneralViewModel? ViewModel => DataContext as GeneralViewModel;

    public GeneralPage(
        IServiceControl serviceControl,
        IBcdService bcdService,
        IRegistrySettingService registrySettingService)
    {
        this.InitializeComponent();
        DataContext = new GeneralViewModel(serviceControl, bcdService, registrySettingService);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.LoadAsync();
    }

    private async void HyperV_Toggled(object sender, RoutedEventArgs e)
    {
        // Ignore re-fired Toggled when the VM reverts the switch after a failed apply.
        if (ViewModel is null || sender is not ToggleSwitch toggle || toggle.IsOn == ViewModel.HyperVOn)
            return;
        await ViewModel.SetHyperVAsync(toggle.IsOn);
    }

    private async void Spooler_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not ToggleSwitch toggle || toggle.IsOn == ViewModel.SpoolerOn)
            return;
        await ViewModel.SetSpoolerAsync(toggle.IsOn);
    }

    private async void Rdp_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not ToggleSwitch toggle || toggle.IsOn == ViewModel.RdpOn)
            return;
        await ViewModel.SetRdpAsync(toggle.IsOn);
    }
}
