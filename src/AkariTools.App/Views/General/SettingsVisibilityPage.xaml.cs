using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.General;

public sealed partial class SettingsVisibilityPage : Page
{
    public SettingsVisibilityViewModel? ViewModel => DataContext as SettingsVisibilityViewModel;

    public SettingsVisibilityPage(IRegistrySettingService registrySettingService)
    {
        this.InitializeComponent();
        DataContext = new SettingsVisibilityViewModel(registrySettingService);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.LoadAsync();
    }

    private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not ToggleSwitch toggle)
            return;

        // Find the row from the DataContext of the toggle.
        if (toggle.DataContext is SettingsPageRow row)
            await ViewModel.ToggleCommand.ExecuteAsync(row);
    }
}
