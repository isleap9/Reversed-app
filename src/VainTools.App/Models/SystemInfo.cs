namespace VainTools.App.Models;

/// <summary>
/// A snapshot of machine information shown on the Home page.
///
/// Every field is independently resolved: a failing WMI query leaves its own field
/// at the "Unknown …" placeholder rather than failing the whole snapshot, matching
/// the real Vain Toolbox behaviour.
/// </summary>
public sealed record SystemInfo
{
    public string Cpu { get; init; } = "Unknown CPU";

    public string MotherboardManufacturer { get; init; } = "Unknown Motherboard";

    public string MotherboardModel { get; init; } = string.Empty;

    public string OperatingSystem { get; init; } = "Unknown OS";

    public string OsVersion { get; init; } = string.Empty;

    public string InstalledRam { get; init; } = "Unknown RAM";

    public string Gpu { get; init; } = "Unknown GPU";

    public string GpuDriverVersion { get; init; } = string.Empty;

    /// <summary>"Manufacturer Model", or just the manufacturer when the model is unknown.</summary>
    public string Motherboard =>
        string.IsNullOrWhiteSpace(MotherboardModel)
            ? MotherboardManufacturer
            : $"{MotherboardManufacturer} {MotherboardModel}".Trim();

    /// <summary>True when no GPU was reported at all.</summary>
    public bool HasGpu => !string.Equals(Gpu, "Unknown GPU", StringComparison.Ordinal);

    /// <summary>Renders the driver version the way the real app labels it.</summary>
    public string GpuDriverDisplay =>
        string.IsNullOrWhiteSpace(GpuDriverVersion) ? "—" : GpuDriverVersion;

    public static SystemInfo Unknown { get; } = new();
}
