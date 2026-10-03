using Microsoft.UI.Xaml.Controls;
using VainTools.Profiles;
using VainTools.Services;

namespace VainTools.Pages;

/// <summary>
/// Code-behind for ProfilesPage.
/// Sets up the DataContext with the ViewModel and handles factory preset button clicks.
/// </summary>
public sealed partial class ProfilesPage : Page
{
    /// <summary>
    /// The ViewModel instance for this page.
    /// </summary>
    public ProfileViewModel ViewModel { get; private set; }

    public ProfilesPage()
    {
        this.InitializeComponent();

        // Create the ViewModel with the profile service
        var profileService = new ProfileService();
        ViewModel = new ProfileViewModel(profileService);

        // Set the DataContext for data binding
        this.DataContext = ViewModel;
    }

    /// <summary>
    /// Handles the Factory Default preset button click.
    /// </summary>
    private async void FactoryDefaultButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(0);
    }

    /// <summary>
    /// Handles the Gaming preset button click.
    /// </summary>
    private async void GamingButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(1);
    }

    /// <summary>
    /// Handles the Silent preset button click.
    /// </summary>
    private async void SilentButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ApplyPresetByIdAsync(2);
    }

    /// <summary>
    /// Applies a preset profile by its ID.
    /// </summary>
    /// <param name="profileId">The profile ID to apply.</param>
    private async System.Threading.Tasks.Task ApplyPresetByIdAsync(int profileId)
    {
        var service = new ProfileService();
        var profile = await service.GetProfileAsync(profileId);

        if (profile != null)
        {
            ViewModel.SelectedProfile = profile;
            await service.ApplyProfileAsync(profile);
            await service.SetActiveProfileAsync(profileId);
        }
    }
}
