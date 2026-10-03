using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VainTools.Core;
using VainTools.Models;
using VainTools.Services;

namespace VainTools.Profiles;

/// <summary>
/// ViewModel for the Profiles page.
/// Manages GPU profile listing, selection, creation, deletion, and application.
/// </summary>
public class ProfileViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private ObservableCollection<GpuProfile> _profiles = new();
    private GpuProfile? _selectedProfile;
    private bool _isLoading;
    private string _statusMessage = "Ready";

    /// <summary>
    /// Initializes a new instance of the ProfileViewModel.
    /// </summary>
    /// <param name="profileService">The profile service for data operations.</param>
    public ProfileViewModel(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        // Initialize commands
        AddProfileCommand = new RelayCommand(async () => await AddProfileAsync());
        DeleteProfileCommand = new RelayCommand(async () => await DeleteProfileAsync(), () => SelectedProfile != null);
        ApplyProfileCommand = new RelayCommand(async () => await ApplyProfileAsync(), () => SelectedProfile != null);
        NewProfileCommand = new RelayCommand(async () => await NewProfileAsync());
        RefreshCommand = new RelayCommand(async () => await LoadProfilesAsync());

        // Load profiles on initialization
        _ = LoadProfilesAsync();
    }

    #region Properties

    /// <summary>
    /// Collection of all available GPU profiles.
    /// </summary>
    public ObservableCollection<GpuProfile> Profiles
    {
        get => _profiles;
        private set => SetProperty(ref _profiles, value);
    }

    /// <summary>
    /// Currently selected profile.
    /// </summary>
    public GpuProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                // Raise CanExecuteChanged for commands that depend on selection
                (DeleteProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ApplyProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Whether a loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Status message for the UI.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to add a new profile.
    /// </summary>
    public ICommand AddProfileCommand { get; }

    /// <summary>
    /// Command to delete the selected profile.
    /// </summary>
    public ICommand DeleteProfileCommand { get; }

    /// <summary>
    /// Command to apply the selected profile.
    /// </summary>
    public ICommand ApplyProfileCommand { get; }

    /// <summary>
    /// Command to create a new profile from current settings.
    /// </summary>
    public ICommand NewProfileCommand { get; }

    /// <summary>
    /// Command to refresh the profile list.
    /// </summary>
    public ICommand RefreshCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Loads all profiles from the service.
    /// </summary>
    public async Task LoadProfilesAsync()
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

            // Select first profile if none selected
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

    /// <summary>
    /// Adds a new profile.
    /// </summary>
    private async Task AddProfileAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating new profile...";

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

    /// <summary>
    /// Deletes the selected profile.
    /// </summary>
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Deleting profile: {SelectedProfile.Name}...";

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

    /// <summary>
    /// Applies the selected profile.
    /// </summary>
    private async Task ApplyProfileAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Applying profile: {SelectedProfile.Name}...";

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

    /// <summary>
    /// Creates a new profile from current GPU settings.
    /// </summary>
    private async Task NewProfileAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating profile from current settings...";

            var profile = await _profileService.CreateProfileFromCurrentAsync("Custom Profile");
            Profiles.Add(profile);
            SelectedProfile = profile;

            StatusMessage = $"Created profile: {profile.Name}";
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

    #endregion
}

/// <summary>
/// Simple RelayCommand implementation for ICommand.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
        _execute = () => _ = execute();
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
