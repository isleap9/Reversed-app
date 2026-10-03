using System.Runtime.InteropServices;

namespace VainTools.Modules;

public static class GpuModule
{
    // NvAPI imports
    [DllImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface")]
    public static extern IntPtr NvapiQueryInterface(uint id);

    // ADLX imports
    [DllImport("adlx.dll", EntryPoint = "ADLX_Init")]
    public static extern int AdlxInit();

    [DllImport("adlx.dll", EntryPoint = "ADLX_Terminate")]
    public static extern int AdlxTerminate();

    public static bool IsNvapiAvailable => File.Exists("nvapi64.dll") || File.Exists("nvapi.dll");
    public static bool IsAdlxAvailable => File.Exists("adlx64.dll") || File.Exists("adlx.dll");

    public enum NvFeature
    {
        SystemClockOffset,
        XbarClockOffset,
        VoltageBoost,
        ThermalLimit,
        DynamicPstates,
        ThermalSlowdown,
        FanAuto,
        FanCurve,
        PowerLimit,
        VfCurve,
        HueAngle,
        DigitalVibrance,
        VfLockUV,
        PowerLimitW,
    }

    public static Dictionary<string, object> GetCurrentSettings()
    {
        var settings = new Dictionary<string, object>();
        if (IsNvapiAvailable)
        {
            settings["gpuMinFreqMHz"] = 0;
            settings["gpuMaxFreqMHz"] = 0;
            settings["gpuVoltage"] = 0;
            settings["powerLimitPct"] = 100;
            settings["nvFanCurveEnabled"] = false;
            settings["nvFanAuto"] = true;
            settings["nvFanTargetId"] = 0;
            settings["nvFanUpdateMs"] = 100;
            settings["nvFanSmoothing"] = 50;
            settings["nvFanRamp"] = 0;
            settings["nvHueAngle"] = 0;
            settings["nvDigitalVibrance"] = 50;
            settings["nvVfLockUV"] = false;
            settings["nvPowerLimitW"] = 0;
        }
        return settings;
    }

    public static void SetSetting(string key, object value)
    {
        // Apply setting via NvAPI/NVML/ADLX
        switch (key)
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
                break;
        }
    }
}