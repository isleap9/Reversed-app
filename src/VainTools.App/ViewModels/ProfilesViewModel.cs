using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VainTools.Framework;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;
using VainTools.Services;

namespace VainTools.App.ViewModels;

public partial class ProfilesViewModel : ViewModelBase
{
    private readonly IProfileService _profileService;

    [ObservableProperty]
    private ObservableCollection<Models.GpuProfile> _profiles = new();

    [ObservableProperty]
    private Models.GpuProfile? _selectedProfile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ProfilesViewModel(IProfileService profileService)
    {
        _profileService = profileService;
        _ = LoadProfilesAsync();
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading profiles...";
            var profiles = await _profileService.GetAllProfilesAsync();

            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            if (SelectedProfile == null && Profiles.Count > 0)
            {
                SelectedProfile = Profiles[0];
            }

            StatusMessage = $"Loaded {Profiles.Count} profile(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading profiles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddProfileAsync()
    {
        try
        {
            IsLoading = true;
            var newProfile = await _profileService.CreateProfileFromCurrentAsync("New Profile");
            Profiles.Add(newProfile);
            SelectedProfile = newProfile;
            StatusMessage = $"Created profile: {newProfile.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null) return;
        try
        {
            IsLoading = true;
            await _profileService.DeleteProfileAsync(SelectedProfile.Id);
            Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
            StatusMessage = "Profile deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyProfileAsync()
    {
        if (SelectedProfile == null) return;
        try
        {
            IsLoading = true;
            await _profileService.ApplyProfileAsync(SelectedProfile);
            await _profileService.SetActiveProfileAsync(SelectedProfile.Id);
            StatusMessage = $"Applied profile: {SelectedProfile.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}