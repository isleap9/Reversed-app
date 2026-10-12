using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

/// <summary>Reads zones via TimeZoneInfo; writes via tzutil through ICliRunner.</summary>
public interface ITimeZoneService
{
    /// <summary>All system zones as (Id, DisplayName) options.</summary>
    IReadOnlyList<TimeZoneOption> GetTimeZones();

    /// <summary>Current zone key (TimeZoneInfo.Local.Id).</summary>
    string? GetCurrentKey();

    /// <summary>Sets the zone via tzutil; false on failure.</summary>
    bool SetTimeZone(string key);
}
