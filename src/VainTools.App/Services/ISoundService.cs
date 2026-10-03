namespace VainTools.App.Services;

/// <summary>Service for audio device enumeration, volume control, and audio settings.</summary>
public interface ISoundService
{
    /// <summary>Gets all active audio render devices.</summary>
    Task<List<AudioDevice>> GetDevicesAsync();

    /// <summary>Gets volume information for a specific device.</summary>
    Task<VolumeInfo> GetVolumeInfoAsync(string deviceId);

    /// <summary>Sets the master volume level for a device (0.0 to 1.0).</summary>
    Task SetVolumeAsync(string deviceId, float level);

    /// <summary>Sets the mute state for a device.</summary>
    Task SetMuteAsync(string deviceId, bool mute);

    /// <summary>Gets spatial audio settings from the registry.</summary>
    Task<SpatialAudioSettings> GetSpatialAudioSettingsAsync();

    /// <summary>Sets spatial audio settings in the registry.</summary>
    Task SetSpatialAudioSettingsAsync(SpatialAudioSettings settings);

    /// <summary>Gets audio enhancement settings from the registry.</summary>
    Task<AudioEnhancementSettings> GetEnhancementSettingsAsync();

    /// <summary>Sets audio enhancement settings in the registry.</summary>
    Task SetEnhancementSettingsAsync(AudioEnhancementSettings settings);
}

/// <summary>Represents an audio endpoint device.</summary>
public record AudioDevice(string Id, string Name, bool IsDefault, bool IsActive);

/// <summary>Volume information for an audio device.</summary>
public record VolumeInfo(float Level, bool IsMute, float Min, float Max);

/// <summary>Spatial audio settings.</summary>
public record SpatialAudioSettings(bool Enabled, int Type);

/// <summary>Audio enhancement settings.</summary>
public record AudioEnhancementSettings(
    bool Enabled,
    bool LoudnessEqualization,
    bool BassBoost,
    bool VirtualSurround,
    bool RoomCorrection);
