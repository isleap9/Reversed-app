using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using VainTools.Models;

namespace VainTools.Services;

/// <summary>
/// JSON file-based implementation of IProfileService.
/// Persists profiles to a JSON file in the app data directory.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly string _profilesDirectory;
    private readonly string _profilesFilePath;
    private readonly string _activeProfileFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the ProfileService.
    /// </summary>
    public ProfileService()
    {
        // Use ApplicationData for persistent storage
        _profilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VainTools", "Profiles");

        _profilesFilePath = Path.Combine(_profilesDirectory, "profiles.json");
        _activeProfileFilePath = Path.Combine(_profilesDirectory, "active_profile.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // Ensure directory exists
        Directory.CreateDirectory(_profilesDirectory);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GpuProfile>> GetAllProfilesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_profilesFilePath))
            {
                // Create default profiles on first run
                var defaults = CreateDefaultProfiles();
                await SaveProfilesToFileAsync(defaults);
                return defaults;
            }

            var json = await File.ReadAllTextAsync(_profilesFilePath);
            var profiles = JsonSerializer.Deserialize<List<GpuProfile>>(json, _jsonOptions);

            if (profiles == null || profiles.Count == 0)
            {
                // File exists but is empty or corrupted, recreate defaults
                var defaults = CreateDefaultProfiles();
                await SaveProfilesToFileAsync(defaults);
                return defaults;
            }

            return profiles;
        }
        catch (JsonException)
        {
            // Corrupted JSON, recreate defaults
            var defaults = CreateDefaultProfiles();
            await SaveProfilesToFileAsync(defaults);
            return defaults;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<GpuProfile?> GetProfileAsync(int profileId)
    {
        var profiles = await GetAllProfilesAsync();
        return profiles.FirstOrDefault(p => p.Id == profileId);
    }

    /// <inheritdoc />
    public async Task SaveProfileAsync(GpuProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        await _fileLock.WaitAsync();
        try
        {
            var profiles = (await GetAllProfilesAsync()).ToList();

            // Update timestamp
            profile.UpdatedAt = DateTime.UtcNow;

            // Find existing profile or add new
            var existingIndex = profiles.FindIndex(p => p.Id == profile.Id);
            if (existingIndex >= 0)
            {
                profiles[existingIndex] = profile;
            }
            else
            {
                // Assign new ID if not set
                if (profile.Id == 0 && profiles.Count > 0)
                {
                    profile.Id = profiles.Max(p => p.Id) + 1;
                }
                profiles.Add(profile);
            }

            await SaveProfilesToFileAsync(profiles);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(int profileId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var profiles = (await GetAllProfilesAsync()).ToList();
            var profileToRemove = profiles.FirstOrDefault(p => p.Id == profileId);

            if (profileToRemove != null)
            {
                profiles.Remove(profileToRemove);
                await SaveProfilesToFileAsync(profiles);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public Task ApplyProfileAsync(GpuProfile profile)
    {
        // Placeholder: In a future phase, this would apply settings via NVML
        // For now, just log the action
        System.Diagnostics.Debug.WriteLine($"Applying profile: {profile.Name} (ID: {profile.Id})");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<GpuProfile> CreateProfileFromCurrentAsync(string name)
    {
        var profiles = await GetAllProfilesAsync();
        var newId = profiles.Count > 0 ? profiles.Max(p => p.Id) + 1 : 0;

        var profile = new GpuProfile
        {
            Id = newId,
            Name = name,
            Description = $"Custom profile created on {DateTime.UtcNow:yyyy-MM-dd}",
            IsDefault = false,
            ActiveProfileId = newId,
            StartupProfileId = newId,
            Settings = new List<GpuSetting>
            {
                new() { Key = "nvCoreOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvMemoryOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvFanSpeed", Value = 0, Unit = "%" },
                new() { Key = "nvPowerLimit", Value = 0, Unit = "W" }
            }
        };

        await SaveProfileAsync(profile);
        return profile;
    }

    /// <inheritdoc />
    public async Task<int> GetActiveProfileIdAsync()
    {
        try
        {
            if (File.Exists(_activeProfileFilePath))
            {
                var json = await File.ReadAllTextAsync(_activeProfileFilePath);
                var data = JsonSerializer.Deserialize<ActiveProfileData>(json, _jsonOptions);
                if (data != null)
                {
                    return data.ActiveProfileId;
                }
            }
        }
        catch
        {
            // If file is corrupted, return default
        }

        return 0; // Default to Factory Default
    }

    /// <inheritdoc />
    public async Task SetActiveProfileAsync(int profileId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var data = new ActiveProfileData { ActiveProfileId = profileId, UpdatedAt = DateTime.UtcNow };
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_activeProfileFilePath, json);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Creates the four default factory profiles.
    /// </summary>
    /// <returns>List of default factory profiles.</returns>
    public static List<GpuProfile> CreateDefaultProfiles()
    {
        var now = DateTime.UtcNow;

        var factoryDefault = new GpuProfile
        {
            Id = 0,
            Name = "Factory Default",
            Description = "Stock GPU settings with no modifications",
            IsDefault = true,
            ActiveProfileId = 0,
            StartupProfileId = 0,
            CreatedAt = now,
            UpdatedAt = now,
            Settings = new List<GpuSetting>
            {
                new() { Key = "nvCoreOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvMemoryOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvFanSpeed", Value = 0, Unit = "%" },
                new() { Key = "nvPowerLimit", Value = 0, Unit = "W" }
            }
        };

        var gaming = new GpuProfile
        {
            Id = 1,
            Name = "Gaming",
            Description = "Optimized for maximum gaming performance",
            IsDefault = false,
            ActiveProfileId = 1,
            StartupProfileId = 1,
            CreatedAt = now,
            UpdatedAt = now,
            Settings = new List<GpuSetting>
            {
                new() { Key = "nvCoreOffset", Value = 100, Unit = "MHz" },
                new() { Key = "nvMemoryOffset", Value = 200, Unit = "MHz" },
                new() { Key = "nvFanSpeed", Value = 80, Unit = "%" },
                new() { Key = "nvPowerLimit", Value = 250, Unit = "W" }
            }
        };

        var silent = new GpuProfile
        {
            Id = 2,
            Name = "Silent",
            Description = "Optimized for quiet operation",
            IsDefault = false,
            ActiveProfileId = 2,
            StartupProfileId = 2,
            CreatedAt = now,
            UpdatedAt = now,
            Settings = new List<GpuSetting>
            {
                new() { Key = "nvCoreOffset", Value = -50, Unit = "MHz" },
                new() { Key = "nvMemoryOffset", Value = -100, Unit = "MHz" },
                new() { Key = "nvFanSpeed", Value = 30, Unit = "%" },
                new() { Key = "nvPowerLimit", Value = 150, Unit = "W" }
            }
        };

        var custom = new GpuProfile
        {
            Id = 3,
            Name = "Custom",
            Description = "Empty template for custom profiles",
            IsDefault = false,
            ActiveProfileId = 3,
            StartupProfileId = 3,
            CreatedAt = now,
            UpdatedAt = now,
            Settings = new List<GpuSetting>
            {
                new() { Key = "nvCoreOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvMemoryOffset", Value = 0, Unit = "MHz" },
                new() { Key = "nvFanSpeed", Value = 0, Unit = "%" },
                new() { Key = "nvPowerLimit", Value = 0, Unit = "W" }
            }
        };

        return new List<GpuProfile> { factoryDefault, gaming, silent, custom };
    }

    /// <summary>
    /// Saves the profile list to the JSON file.
    /// </summary>
    private async Task SaveProfilesToFileAsync(List<GpuProfile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles, _jsonOptions);
        await File.WriteAllTextAsync(_profilesFilePath, json);
    }

    /// <summary>
    /// Helper class for active profile persistence.
    /// </summary>
    private class ActiveProfileData
    {
        [JsonPropertyName("activeProfileId")]
        public int ActiveProfileId { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
