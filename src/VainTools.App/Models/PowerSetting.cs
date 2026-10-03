namespace VainTools.App.Models;

/// <summary>
/// A power setting within a power plan.
/// </summary>
public record PowerSetting(
    string Guid,
    string Name,
    string CategoryGuid,
    string CategoryName,
    string CurrentAcValue,
    string CurrentDcValue,
    IReadOnlyList<string> PossibleValues);
