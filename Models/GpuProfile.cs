namespace VainTools.Models;

public class GpuProfile
{
    public string Name { get; set; } = "";
    public int ActiveProfileId { get; set; }
    public int StartupProfileId { get; set; }
    public List<GpuSetting> Settings { get; set; } = new();
}

public class GpuSetting
{
    public string Key { get; set; } = "";
    public object Value { get; set; } = 0;
    public string Unit { get; set; } = "";
}

public class NvFanCurvePoint
{
    public int Temperature { get; set; }
    public int SpeedPercent { get; set; }
}

public class NvVfPoint
{
    public int VoltageMv { get; set; }
    public int OffsetMhz { get; set; }
}