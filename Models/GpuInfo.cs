namespace VainTools.Models;

/// <summary>
/// Represents a GPU device with basic identification information.
/// </summary>
public class GpuInfo
{
    /// <summary>
    /// The index/ID of the GPU in the system.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name of the GPU (e.g., "NVIDIA GeForce RTX 4090").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The NVML device handle used for P/Invoke calls.
    /// </summary>
    public IntPtr Handle { get; set; }

    public override string ToString() => Name;
}
