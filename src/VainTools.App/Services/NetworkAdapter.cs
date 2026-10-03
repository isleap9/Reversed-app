namespace VainTools.App.Services;

/// <summary>
/// Represents a network adapter with its properties.
/// </summary>
public record NetworkAdapter(
    string Name,
    string InterfaceDescription,
    string Status,
    string MacAddress,
    string LinkSpeed);
