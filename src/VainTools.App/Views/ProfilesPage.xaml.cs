using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.Services;

namespace VainTools.App.Views;

public sealed partial class ProfilesPage : Page
{
    public ViewModels.ProfilesViewModel ViewModel { get; }

    public ProfilesPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.ProfilesViewModel>();
        this.DataContext = ViewModel;
    }

    private async void FactoryDefaultButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(0);
    }

    private async void GamingButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(1);
    }

    private async void SilentButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(2);
    }

    private async System.Threading.Tasks.Task ApplyPresetByIdAsync(int profileId)
    {
        var service = App.Services.GetRequiredService<IProfileService>();
        var profile = await service.GetProfileAsync(profileId);

        if (profile != null)
        {
            ViewModel.SelectedProfile = profile;
            await service.ApplyProfileAsync(profile);
            await service.SetActiveProfileAsync(profileId);
        }
    }
}