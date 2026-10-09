namespace VainTools.App.Services;

/// <summary>
/// A Windows optional feature reported by DISM.
/// </summary>
public sealed record OptionalFeature(string Name, string State);
