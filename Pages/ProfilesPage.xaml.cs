using Microsoft.UI.Xaml.Controls;
using VainTools.Models;
using VainTools.Modules;

namespace VainTools.Pages;

public sealed partial class ProfilesPage : Page
{
    public ProfilesPage()
    {
        this.InitializeComponent();
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        var profiles = GovernorModule.GetProfiles();
        ProfilesList.ItemsSource = profiles;
    }

    private void AddProfileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var profiles = GovernorModule.GetProfiles();
        profiles.Add(new GpuProfile { Name = "New Profile", ActiveProfileId = profiles.Count, StartupProfileId = profiles.Count });
        ProfilesList.ItemsSource = null;
        ProfilesList.ItemsSource = profiles;
    }

    private void DeleteProfileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is GpuProfile profile)
        {
            var profiles = GovernorModule.GetProfiles();
            profiles.Remove(profile);
            ProfilesList.ItemsSource = null;
            ProfilesList.ItemsSource = profiles;
        }
    }

    private void ApplyProfileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is GpuProfile profile)
        {
            GovernorModule.ApplyProfile(profile);
        }
    }
}