using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

/// <summary>Reads zones via TimeZoneInfo; writes via tzutil through ICliRunner.</summary>
public sealed class TimeZoneService : ITimeZoneService
{
    private readonly ICliRunner _cliRunner;

    /// <summary>Creates a service that sets zones via the given runner.</summary>
    public TimeZoneService(ICliRunner cliRunner)
    {
        _cliRunner = cliRunner;
    }

    /// <summary>Maps TimeZoneInfo.GetSystemTimeZones() to (Id, DisplayName).</summary>
    public IReadOnlyList<TimeZoneOption> GetTimeZones()
    {
        try
        {
            return TimeZoneInfo.GetSystemTimeZones()
                .Select(z => new TimeZoneOption(z.Id, z.DisplayName))
                .ToList();
        }
        catch
        {
            return Array.Empty<TimeZoneOption>();
        }
    }

    /// <summary>Returns TimeZoneInfo.Local.Id.</summary>
    public string? GetCurrentKey()
    {
        try
        {
            return TimeZoneInfo.Local.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Runs tzutil.exe /s "key"; true when the runner returns output.</summary>
    // R-1 live check 2026-10-12: tzutil /g -> "W. Europe Standard Time" (exit 0);
    // tzutil.exe /s "W. Europe Standard Time" (same-key no-op) -> exit 0, key unchanged.
    public bool SetTimeZone(string key)
    {
        try
        {
            return _cliRunner.Run("tzutil.exe", $"/s \"{key}\"") is not null;
        }
        catch
        {
            return false;
        }
    }
}
