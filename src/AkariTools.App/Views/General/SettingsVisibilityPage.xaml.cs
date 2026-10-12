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
        // OneWay binding: the row still holds the previous value when a user
        // toggle fires, so the switch value is passed explicitly. Revert
        // echoes (the switch snapping back after a failed apply) carry the
        // value already shown and are ignored.
        if (ViewModel is null || sender is not ToggleSwitch toggle
            || toggle.DataContext is not SettingsPageRow row
            || toggle.IsOn == row.IsHidden)
            return;

        await ViewModel.SetHiddenAsync(row, toggle.IsOn);
    }
}
