using System.Collections.Generic;
using System.Threading.Tasks;
using VainTools.Models;

namespace VainTools.Services;

/// <summary>
/// Interface for profile management.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets all available GPU profiles.
    /// </summary>
    Task<IReadOnlyList<GpuProfile>> GetAllProfilesAsync();

    /// <summary>
    /// Gets a specific profile by ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<GpuProfile?> GetProfileAsync(int profileId);

    /// <summary>
    /// Saves a profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    Task SaveProfileAsync(GpuProfile profile);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    /// <param name="profileId">The profile ID to delete.</param>
    Task DeleteProfileAsync(int profileId);

    /// <summary>
    /// Applies a profile's settings.
    /// </summary>
    /// <param name="profile">The profile to apply.</param>
    Task ApplyProfileAsync(GpuProfile profile);

    /// <summary>
    /// Creates a new profile from current settings.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The created profile.</returns>
    Task<GpuProfile> CreateProfileFromCurrentAsync(string name);

    /// <summary>
    /// Gets the currently active profile ID.
    /// </summary>
    Task<int> GetActiveProfileIdAsync();

    /// <summary>
    /// Sets the active profile.
    /// </summary>
    /// <param name="profileId">The profile ID to set as active.</param>
    Task SetActiveProfileAsync(int profileId);
}