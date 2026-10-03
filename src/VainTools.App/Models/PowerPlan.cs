namespace VainTools.App.Models;

/// <summary>
/// A Windows power plan.
/// </summary>
public record PowerPlan(Guid Guid, string Name, bool IsActive);
