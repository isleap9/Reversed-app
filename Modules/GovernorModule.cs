using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VainTools.Models;

namespace VainTools.Modules;

public static class GovernorModule
{
    // NVIDIA NVML imports
    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    public static extern int NvmlInit();

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    public static extern int NvmlShutdown();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2")]
    public static extern int NvmlDeviceGetCount(out uint count);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    public static extern int NvmlDeviceGetHandleByIndex(uint index, out IntPtr handle);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
    public static extern int NvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    public static extern int NvmlDeviceGetTemperature(IntPtr device, int sensorType, out int temp);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetClockOffsets")]
    public static extern int NvmlDeviceGetClockOffsets(IntPtr device, out int coreOffset, out int memoryOffset);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetClockOffsets")]
    public static extern int NvmlDeviceSetClockOffsets(IntPtr device, int coreOffset, int memoryOffset);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementLimit")]
    public static extern int NvmlDeviceGetPowerManagementLimit(IntPtr device, out int limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceSetPowerManagementLimit")]
    public static extern int NvmlDeviceSetPowerManagementLimit(IntPtr device, int limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementDefaultLimit")]
    public static extern int NvmlDeviceGetPowerManagementDefaultLimit(IntPtr device, out int limit);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerManagementLimitConstraints")]
    public static extern int NvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out int min, out int max);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetFanSpeed_v2")]
    public static extern int NvmlDeviceGetFanSpeed_v2(IntPtr device, out uint speed);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPerformanceState")]
    public static extern int NvmlDeviceGetPerformanceState(IntPtr device, out int pstate);

    public const int NVML_TEMPERATURE_GPU = 0;

    public static bool IsNvmlAvailable => File.Exists("nvml.dll") || File.Exists("nvml64_535.dll") || File.Exists("nvml64_545.dll");

    public static List<GpuProfile> GetProfiles()
    {
        return new()
        {
            new() { Name = "Factory Default", ActiveProfileId = 0, StartupProfileId = 0 },
            new() { Name = "Gaming", ActiveProfileId = 1, StartupProfileId = 1 },
            new() { Name = "Silent", ActiveProfileId = 2, StartupProfileId = 2 },
            new() { Name = "Custom", ActiveProfileId = 3, StartupProfileId = 3 },
        };
    }

    public static Dictionary<string, object> GetCurrentSettings()
    {
        var settings = new Dictionary<string, object>
        {
            ["gpuMinFreqMHz"] = 0,
            ["gpuMaxFreqMHz"] = 0,
            ["gpuVoltage"] = 0,
            ["powerLimitPct"] = 100,
            ["nvFanCurveEnabled"] = false,
            ["nvFanAuto"] = true,
            ["nvFanTargetId"] = 0,
            ["nvFanUpdateMs"] = 100,
            ["nvFanSmoothing"] = 50,
            ["nvFanRamp"] = 0,
            ["nvHueAngle"] = 0,
            ["nvDigitalVibrance"] = 50,
            ["nvVfLockUV"] = false,
            ["nvPowerLimitW"] = 0,
            ["nvSysClockOffsetMHz"] = 0,
            ["nvXbarClockOffsetMHz"] = 0,
            ["nvVoltageBoostPct"] = 0,
            ["nvThermalLimitC"] = 0,
            ["nvDynamicPstates"] = true,
            ["nvThermalSlowdown"] = true,
        };

        if (!IsNvmlAvailable) return settings;

        try
        {
            NvmlInit();
            if (NvmlDeviceGetCount(out uint count) == 0 && count > 0)
            {
                if (NvmlDeviceGetHandleByIndex(0, out IntPtr handle) == 0)
                {
                    if (NvmlDeviceGetTemperature(handle, NVML_TEMPERATURE_GPU, out int temp) == 0)
                        settings["gpuTemperature"] = temp;
                    if (NvmlDeviceGetPerformanceState(handle, out int pstate) == 0)
                        settings["gpuPState"] = pstate;
                    if (NvmlDeviceGetFanSpeed_v2(handle, out uint fanSpeed) == 0)
                        settings["nvFanSpeed"] = fanSpeed;
                    if (NvmlDeviceGetPowerManagementLimit(handle, out int powerLimit) == 0)
                        settings["nvPowerLimit"] = powerLimit;
                    if (NvmlDeviceGetPowerManagementDefaultLimit(handle, out int defaultLimit) == 0)
                        settings["nvPowerDefaultLimit"] = defaultLimit;
                }
            }
        }
        catch { }
        return settings;
    }

    public static void ApplyProfile(GpuProfile profile)
    {
        foreach (var setting in profile.Settings)
        {
            ApplySetting(setting);
        }
    }

    public static void ApplySetting(GpuSetting setting)
    {
        switch (setting.Key)
        {
            case "nvSysClockOffsetMHz":
            case "nvXbarClockOffsetMHz":
            case "nvVoltageBoostPct":
            case "nvThermalLimitC":
            case "nvDynamicPstates":
            case "nvThermalSlowdown":
            case "nvFanAuto":
            case "nvFanCurveEnabled":
            case "nvFanTargetId":
            case "nvFanUpdateMs":
            case "nvFanSmoothing":
            case "nvFanRamp":
            case "voltageMv":
            case "offsetMHz":
            case "nvVfCurve":
            case "nvPowerLimitW":
            case "powerLimitPct":
            case "gpuMinFreqMHz":
            case "gpuMaxFreqMHz":
            case "gpuVoltage":
                break;
        }
    }
}