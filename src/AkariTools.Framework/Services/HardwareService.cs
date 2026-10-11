using System.Management;
using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

public sealed class HardwareService : IHardwareService
{
    public Task<HardwareInfo> LoadHardwareInfoAsync() =>
        Task.Run(() => new HardwareInfo(
            Processor: GetProcessor(),
            Graphics: GetGraphics(),
            Memory: GetMemory(),
            Motherboard: GetMotherboard()));

    private static string FirstString(string query, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
            {
                var value = obj[property]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch
        {
            // Fall through to "Unknown".
        }
        return "Unknown";
    }

    private static string GetProcessor() =>
        FirstString("SELECT Name FROM Win32_Processor", "Name");

    private static string GetGraphics() =>
        FirstString("SELECT Name FROM Win32_VideoController WHERE PNPDeviceID LIKE 'PCI%'", "Name");

    private static string GetMotherboard()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Product, Manufacturer FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var product = obj["Product"]?.ToString()?.Trim();
                var manufacturer = obj["Manufacturer"]?.ToString()?.Trim();
                var combined = string.Join(" ", new[] { manufacturer, product }
                    .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                if (!string.IsNullOrWhiteSpace(combined))
                    return combined;
            }
        }
        catch
        {
            // Fall through to "Unknown".
        }
        return "Unknown";
    }

    private static string GetMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Capacity, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory");
            long totalBytes = 0;
            var moduleSpeeds = new List<uint>();
            int modules = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                modules++;
                if (ulong.TryParse(obj["Capacity"]?.ToString(), out var bytes))
                    totalBytes += (long)bytes;
                var speed = obj["Speed"] ?? obj["ConfiguredClockSpeed"];
                if (uint.TryParse(speed?.ToString(), out var mhz) && mhz > 0)
                    moduleSpeeds.Add(mhz);
            }

            if (modules == 0 || totalBytes <= 0)
                return "Unknown";

            double gb = totalBytes / (1024.0 * 1024.0 * 1024.0);
            var gbText = gb >= 10 ? $"{gb:0}" : $"{gb:0.#}";
            var speedText = moduleSpeeds.Count > 0 ? $" @ {moduleSpeeds.Max()} MHz" : string.Empty;
            return $"{gbText} GB ({modules}x){speedText}";
        }
        catch
        {
            return "Unknown";
        }
    }
}
