namespace AkariTools.Framework.Models;

public sealed record HardwareInfo(
    string Processor = "Unknown",
    string Graphics = "Unknown",
    string Memory = "Unknown",
    string Motherboard = "Unknown");
