using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VainTools.Models;

namespace VainTools.Services;

/// <summary>
/// NVML-based implementation of IGpuService.
/// Uses P/Invoke to call NVIDIA Management Library functions.
/// Gracefully degrades when NVML is not available.
/// </summary>
public class GpuService : IGpuService
{
    // NVML return code: success
    private const int NVML_SUCCESS = 0;

    // NVML temperature sensor type: GPU core
    private const int NVML_TEMPERATURE_GPU = 0;

    // NVML clock types
    private const int NVML_CLOCK_GRAPHICS = 0;
    private const int NVML_CLOCK_MEM = 2;

    // NVML utilization sample period in microseconds
    private const int NVML_UTILIZATION_SAMPLE_PERIOD_US = 1000000;

    // P/Invoke declarations for NVML
    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    private static extern int NvmlInit();

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int NvmlShutdown();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2")]
    private static extern int NvmlDeviceGetCount(out uint count);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static extern int NvmlDeviceGetHandleByIndex(uint index, out IntPtr handle);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
    private static extern int NvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    private static extern int NvmlDeviceGetTemperature(IntPtr device, int sensorType, out int temp);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetFanSpeed_v2")]
    private static extern int NvmlDeviceGetFanSpeed_v2(IntPtr device, out uint speed);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetClockInfo")]
    private static extern int NvmlDeviceGetClockInfo(IntPtr device, int clockType, out uint clock);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerUsage")]
    private static extern int NvmlDeviceGetPowerUsage(IntPtr device, out uint power);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static extern int NvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementLimit")]
    private static extern int NvmlDeviceGetPowerManagementLimit(IntPtr device, out uint limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetPowerManagementLimit")]
    private static extern int NvmlDeviceSetPowerManagementLimit(IntPtr device, uint limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementDefaultLimit")]
    private static extern int NvmlDeviceGetPowerManagementDefaultLimit(IntPtr device, out uint limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementLimitConstraints")]
    private static extern int NvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint min, out uint max);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPerformanceState")]
    private static extern int NvmlDeviceGetPerformanceState(IntPtr device, out int pstate);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetClockOffsets")]
    private static extern int NvmlDeviceGetClockOffsets(IntPtr device, out int coreOffset, out int memoryOffset);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetClockOffsets")]
    private static extern int NvmlDeviceSetClockOffsets(IntPtr device, int coreOffset, int memoryOffset);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetFanSpeed_v2")]
    private static extern int NvmlDeviceSetFanSpeed_v2(IntPtr device, uint speed);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetFanCurve")]
    private static extern int NvmlDeviceGetFanCurve(IntPtr device, [Out] NvmlFanCurvePoint[] points, ref int count);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetFanCurve")]
    private static extern int NvmlDeviceSetFanCurve(IntPtr device, NvmlFanCurvePoint[] points, int count);

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint Gpu;
        public uint Memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlFanCurvePoint
    {
        public int Temperature;
        public int SpeedPercent;
    }

    private bool _initialized;
    private readonly object _initLock = new();

    /// <summary>
    /// Checks if NVML is available on this system.
    /// </summary>
    public static bool IsNvmlAvailable()
    {
        try
        {
            return File.Exists("nvml.dll") ||
                   File.Exists("nvml64_535.dll") ||
                   File.Exists("nvml64_545.dll") ||
                   File.Exists("nvml64_550.dll") ||
                   File.Exists("nvml64_555.dll") ||
                   File.Exists("nvml64_560.dll") ||
                   File.Exists("nvml64_565.dll") ||
                   File.Exists("nvml64_570.dll") ||
                   File.Exists("nvml64_575.dll") ||
                   File.Exists("nvml64_580.dll") ||
                   File.Exists("nvml64_585.dll") ||
                   File.Exists("nvml64_590.dll") ||
                   File.Exists("nvml64_595.dll") ||
                   File.Exists("nvml64_600.dll");
        }
        catch
        {
            return false;
        }
    }

    private bool EnsureInitialized()
    {
        if (_initialized) return true;

        lock (_initLock)
        {
            if (_initialized) return true;

            if (!IsNvmlAvailable()) return false;

            try
            {
                int result = NvmlInit();
                if (result == NVML_SUCCESS)
                {
                    _initialized = true;
                    return true;
                }
            }
            catch
            {
                // NVML not available or failed to initialize
            }
        }

        return false;
    }

    public Task<int> GetDeviceCountAsync()
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized()) return 0;

            try
            {
                int result = NvmlDeviceGetCount(out uint count);
                return result == NVML_SUCCESS ? (int)count : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<IntPtr> GetDeviceHandleAsync(int index)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized()) return IntPtr.Zero;

            try
            {
                int result = NvmlDeviceGetHandleByIndex((uint)index, out IntPtr handle);
                return result == NVML_SUCCESS ? handle : IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        });
    }

    public Task<string> GetDeviceNameAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return "Unknown GPU";

            try
            {
                var sb = new StringBuilder(256);
                int result = NvmlDeviceGetName(deviceHandle, sb, 256);
                return result == NVML_SUCCESS ? sb.ToString() : "Unknown GPU";
            }
            catch
            {
                return "Unknown GPU";
            }
        });
    }

    public Task<int> GetTemperatureAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetTemperature(deviceHandle, NVML_TEMPERATURE_GPU, out int temp);
                return result == NVML_SUCCESS ? temp : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<int> GetFanSpeedAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetFanSpeed_v2(deviceHandle, out uint speed);
                return result == NVML_SUCCESS ? (int)speed : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<int> GetCoreClockAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetClockInfo(deviceHandle, NVML_CLOCK_GRAPHICS, out uint clock);
                return result == NVML_SUCCESS ? (int)clock : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<int> GetMemoryClockAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetClockInfo(deviceHandle, NVML_CLOCK_MEM, out uint clock);
                return result == NVML_SUCCESS ? (int)clock : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<int> GetPowerConsumptionAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetPowerUsage(deviceHandle, out uint power);
                // NVML returns power in milliwatts, convert to watts
                return result == NVML_SUCCESS ? (int)(power / 1000) : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task<int> GetPowerLimitAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetPowerManagementLimit(deviceHandle, out uint limit);
                // NVML returns power in milliwatts, convert to watts
                return result == NVML_SUCCESS ? (int)(limit / 1000) : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    public Task SetPowerLimitAsync(IntPtr deviceHandle, int powerLimitWatts)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return;

            try
            {
                // Convert watts to milliwatts for NVML
                uint limitMw = (uint)(powerLimitWatts * 1000);
                NvmlDeviceSetPowerManagementLimit(deviceHandle, limitMw);
            }
            catch
            {
                // Silently fail if power limit cannot be set
            }
        });
    }

    public Task<(int coreOffset, int memoryOffset)> GetClockOffsetsAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return (0, 0);

            try
            {
                int result = NvmlDeviceGetClockOffsets(deviceHandle, out int coreOffset, out int memoryOffset);
                return result == NVML_SUCCESS ? (coreOffset, memoryOffset) : (0, 0);
            }
            catch
            {
                return (0, 0);
            }
        });
    }

    public Task SetClockOffsetsAsync(IntPtr deviceHandle, int coreOffset, int memoryOffset)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return;

            try
            {
                NvmlDeviceSetClockOffsets(deviceHandle, coreOffset, memoryOffset);
            }
            catch
            {
                // Silently fail if clock offsets cannot be set
            }
        });
    }

    public Task SetFanSpeedAsync(IntPtr deviceHandle, int speedPercent)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return;

            try
            {
                NvmlDeviceSetFanSpeed_v2(deviceHandle, (uint)speedPercent);
            }
            catch
            {
                // Silently fail if fan speed cannot be set
            }
        });
    }

    public Task<List<NvFanCurvePoint>> GetFanCurveAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return new List<NvFanCurvePoint>();

            try
            {
                var points = new NvmlFanCurvePoint[20];
                int count = 20;
                int result = NvmlDeviceGetFanCurve(deviceHandle, points, ref count);
                if (result == NVML_SUCCESS && count > 0)
                {
                    var curve = new List<NvFanCurvePoint>(count);
                    for (int i = 0; i < count && i < points.Length; i++)
                    {
                        curve.Add(new NvFanCurvePoint
                        {
                            Temperature = points[i].Temperature,
                            SpeedPercent = points[i].SpeedPercent
                        });
                    }
                    return curve;
                }
            }
            catch
            {
                // Silently fail
            }

            return new List<NvFanCurvePoint>();
        });
    }

    public Task SetFanCurveAsync(IntPtr deviceHandle, List<NvFanCurvePoint> points)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero || points == null || points.Count == 0) return;

            try
            {
                var nativePoints = new NvmlFanCurvePoint[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    nativePoints[i] = new NvmlFanCurvePoint
                    {
                        Temperature = points[i].Temperature,
                        SpeedPercent = points[i].SpeedPercent
                    };
                }
                NvmlDeviceSetFanCurve(deviceHandle, nativePoints, nativePoints.Length);
            }
            catch
            {
                // Silently fail if fan curve cannot be set
            }
        });
    }

    public Task<GpuProfile> CreateProfileFromCurrentSettingsAsync(int profileId, string name)
    {
        return Task.Run(() =>
        {
            var profile = new GpuProfile
            {
                Name = name,
                ActiveProfileId = profileId,
                StartupProfileId = profileId
            };

            if (!EnsureInitialized()) return profile;

            try
            {
                int deviceCount = GetDeviceCountAsync().GetAwaiter().GetResult();
                if (deviceCount > 0)
                {
                    IntPtr handle = GetDeviceHandleAsync(0).GetAwaiter().GetResult();
                    if (handle != IntPtr.Zero)
                    {
                        int temp = GetTemperatureAsync(handle).GetAwaiter().GetResult();
                        int fanSpeed = GetFanSpeedAsync(handle).GetAwaiter().GetResult();
                        int coreClock = GetCoreClockAsync(handle).GetAwaiter().GetResult();
                        int memClock = GetMemoryClockAsync(handle).GetAwaiter().GetResult();
                        int power = GetPowerConsumptionAsync(handle).GetAwaiter().GetResult();
                        int powerLimit = GetPowerLimitAsync(handle).GetAwaiter().GetResult();
                        var (coreOffset, memOffset) = GetClockOffsetsAsync(handle).GetAwaiter().GetResult();

                        profile.Settings = new List<GpuSetting>
                        {
                            new() { Key = "gpuTemperature", Value = temp, Unit = "°C" },
                            new() { Key = "nvFanSpeed", Value = fanSpeed, Unit = "%" },
                            new() { Key = "gpuCoreClock", Value = coreClock, Unit = "MHz" },
                            new() { Key = "gpuMemoryClock", Value = memClock, Unit = "MHz" },
                            new() { Key = "gpuPower", Value = power, Unit = "W" },
                            new() { Key = "nvPowerLimit", Value = powerLimit, Unit = "W" },
                            new() { Key = "nvCoreOffset", Value = coreOffset, Unit = "MHz" },
                            new() { Key = "nvMemoryOffset", Value = memOffset, Unit = "MHz" }
                        };
                    }
                }
            }
            catch
            {
                // Silently fail
            }

            return profile;
        });
    }

    /// <summary>
    /// Gets the GPU utilization percentage.
    /// </summary>
    public Task<int> GetUtilizationAsync(IntPtr deviceHandle)
    {
        return Task.Run(() =>
        {
            if (!EnsureInitialized() || deviceHandle == IntPtr.Zero) return 0;

            try
            {
                int result = NvmlDeviceGetUtilizationRates(deviceHandle, out NvmlUtilization util);
                return result == NVML_SUCCESS ? (int)util.Gpu : 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    /// <summary>
    /// Shuts down NVML and releases resources.
    /// </summary>
    public void Shutdown()
    {
        if (_initialized)
        {
            try
            {
                NvmlShutdown();
            }
            catch
            {
                // Silently fail
            }
            _initialized = false;
        }
    }
}
