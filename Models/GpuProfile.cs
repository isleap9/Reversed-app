using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VainTools.Models;

/// <summary>
/// Represents a GPU profile with settings for clock offsets, fan curves, and power limits.
/// </summary>
public class GpuProfile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Display name of the profile.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Description of the profile.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// Whether this is the default factory profile.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// The currently active profile ID (for legacy compatibility).
    /// </summary>
    [JsonPropertyName("activeProfileId")]
    public int ActiveProfileId { get; set; }

    /// <summary>
    /// The profile ID to use at startup.
    /// </summary>
    [JsonPropertyName("startupProfileId")]
    public int StartupProfileId { get; set; }

    /// <summary>
    /// List of GPU settings for this profile.
    /// </summary>
    [JsonPropertyName("settings")]
    public List<GpuSetting> Settings { get; set; } = new();

    /// <summary>
    /// Timestamp when the profile was created (UTC).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the profile was last updated (UTC).
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a deep copy of this profile.
    /// </summary>
    /// <returns>A new GpuProfile instance with copied values.</returns>
    public GpuProfile Clone()
    {
        var clonedSettings = new List<GpuSetting>();
        foreach (var setting in Settings)
        {
            clonedSettings.Add(new GpuSetting
            {
                Key = setting.Key,
                Value = setting.Value,
                Unit = setting.Unit
            });
        }

        return new GpuProfile
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            IsDefault = this.IsDefault,
            ActiveProfileId = this.ActiveProfileId,
            StartupProfileId = this.StartupProfileId,
            Settings = clonedSettings,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt
        };
    }
}

/// <summary>
    /// Represents a single GPU setting key-value pair.
/// </summary>
public class GpuSetting
{
    /// <summary>
    /// The setting key (e.g., "nvCoreOffset", "nvFanSpeed").
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    /// <summary>
    /// The setting value.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// The unit of the setting (e.g., "MHz", "%", "W").
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "";
}

/// <summary>
    /// Represents a point on a fan curve.
/// </summary>
public class NvFanCurvePoint
{
    /// <summary>
    /// Temperature in degrees Celsius.
    /// </summary>
    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }

    /// <summary>
    /// Fan speed percentage at this temperature.
    /// </summary>
    [JsonPropertyName("speedPercent")]
    public int SpeedPercent { get; set; }
}

/// <summary>
    /// Represents a voltage-frequency point.
/// </summary>
public class NvVfPoint
{
    /// <summary>
    /// Voltage in millivolts.
    /// </summary>
    [JsonPropertyName("voltageMv")]
    public int VoltageMv { get; set; }

    /// <summary>
    /// Clock offset in MHz.
    /// </summary>
    [JsonPropertyName("offsetMhz")]
    public int OffsetMhz { get; set; }
}
