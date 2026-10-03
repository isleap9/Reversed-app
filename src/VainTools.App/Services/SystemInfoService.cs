using System.Globalization;
using System.Management;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Reads machine information via WMI.
///
/// WMI is treated as best-effort: this machine may have a broken repository, a
/// restricted namespace, or simply no value for a given property. Each lookup is
/// isolated so one failure cannot blank the whole Home page.
/// </summary>
public sealed class SystemInfoService : ISystemInfoService
{
    private const string UnknownCpu = "Unknown CPU";
    private const string UnknownGpu = "Unknown GPU";
    private const string UnknownMotherboard = "Unknown Motherboard";
    private const string UnknownRam = "Unknown RAM";

    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(ILogger<SystemInfoService> logger) => _logger = logger;

    /// <inheritdoc />
    public Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => Collect(cancellationToken), cancellationToken);

    private SystemInfo Collect(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cpu = QueryFirst("SELECT Name FROM Win32_Processor", "Name")?.Trim();
        var board = QueryFirst("SELECT Manufacturer, Product FROM Win32_BaseBoard", "Manufacturer")?.Trim();
        var boardModel = QueryFirst("SELECT Manufacturer, Product FROM Win32_BaseBoard", "Product")?.Trim();
        var os = QueryFirst("SELECT Caption, Version FROM Win32_OperatingSystem", "Caption")?.Trim();
        var osVersion = QueryFirst("SELECT Caption, Version FROM Win32_OperatingSystem", "Version")?.Trim();
        var gpu = QueryFirst("SELECT Name FROM Win32_VideoController", "Name")?.Trim();
        var driver = QueryFirst("SELECT DriverVersion FROM Win32_VideoController", "DriverVersion")?.Trim();

        return new SystemInfo
        {
            Cpu = string.IsNullOrWhiteSpace(cpu) ? UnknownCpu : cpu,
            MotherboardManufacturer = string.IsNullOrWhiteSpace(board) ? UnknownMotherboard : board,
            MotherboardModel = boardModel ?? string.Empty,
            OperatingSystem = string.IsNullOrWhiteSpace(os) ? "Unknown OS" : os,
            OsVersion = osVersion ?? string.Empty,
            InstalledRam = FormatInstalledRam(),
            Gpu = string.IsNullOrWhiteSpace(gpu) ? UnknownGpu : gpu,
            GpuDriverVersion = driver ?? string.Empty,
        };
    }

    /// <summary>
    /// Returns the first non-null value of <paramref name="property"/> from the query,
    /// or null when the query fails or yields nothing.
    /// </summary>
    private string? QueryFirst(string query, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            foreach (var item in searcher.Get())
            {
                using (item)
                {
                    var value = item[property]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WMI query failed: {Query} ({Property})", query, property);
        }

        return null;
    }

    /// <summary>
    /// Reads total physical memory and formats it in GiB, e.g. "31.9 GB".
    /// Returns the RAM placeholder when unavailable.
    /// </summary>
    private string FormatInstalledRam()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

            foreach (var item in searcher.Get())
            {
                using (item)
                {
                    if (item["TotalPhysicalMemory"] is { } raw
                        && ulong.TryParse(raw.ToString(), out var bytes)
                        && bytes > 0)
                    {
                        var gib = bytes / 1024d / 1024d / 1024d;
                        return string.Create(
                            CultureInfo.InvariantCulture,
                            $"{gib:0.#} GB");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WMI query failed: TotalPhysicalMemory");
        }

        return UnknownRam;
    }
}
