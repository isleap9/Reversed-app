using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VainTools.Models;

namespace VainTools.Services;

/// <summary>
/// Interface for GPU management services.
/// </summary>
public interface IGpuService
{
    /// <summary>
    /// Gets the number of GPUs in the system.
    /// </summary>
    Task<int> GetDeviceCountAsync();

    /// <summary>
    /// Gets a handle for the GPU at the specified index.
    /// </summary>
    Task<IntPtr> GetDeviceHandleAsync(int index);

    /// <summary>
    /// Gets the name of the GPU.
    /// </summary>
    Task<string> GetDeviceNameAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current temperature in degrees Celsius.
    /// </summary>
    Task<int> GetTemperatureAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current fan speed percentage.
    /// </summary>
    Task<int> GetFanSpeedAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current core clock speed in MHz.
    /// </summary>
    Task<int> GetCoreClockAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current memory clock speed in MHz.
    /// </summary>
    Task<int> GetMemoryClockAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current power consumption in watts.
    /// </summary>
    Task<int> GetPowerConsumptionAsync(IntPtr deviceHandle);

    /// <summary>
    /// Gets the current power limit in watts.
    /// </summary>
    Task<int> GetPowerLimitAsync(IntPtr deviceHandle);

    /// <summary>
    /// Sets the power limit in watts.
    /// </summary>
    Task SetPowerLimitAsync(IntPtr deviceHandle, int powerLimitWatts);

    /// <summary>
    /// Gets clock offsets for the GPU.
    /// </summary>
    Task<(int coreOffset, int memoryOffset)> GetClockOffsetsAsync(IntPtr deviceHandle);

    /// <summary>
    /// Sets clock offsets for the GPU.
    /// </summary>
    Task SetClockOffsetsAsync(IntPtr deviceHandle, int coreOffset, int memoryOffset);

    /// <summary>
    /// Sets the fan speed percentage.
    /// </summary>
    Task SetFanSpeedAsync(IntPtr deviceHandle, int speedPercent);

    /// <summary>
    /// Gets the fan curve points.
    /// </summary>
    Task<List<NvFanCurvePoint>> GetFanCurveAsync(IntPtr deviceHandle);

    /// <summary>
    /// Sets the fan curve points.
    /// </summary>
    Task SetFanCurveAsync(IntPtr deviceHandle, List<NvFanCurvePoint> points);

    /// <summary>
    /// Creates a GPU profile from current settings.
    /// </summary>
    Task<GpuProfile> CreateProfileFromCurrentSettingsAsync(int profileId, string name);
}