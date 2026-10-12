namespace AkariTools.Framework.Models;

public sealed class ToggleSetting
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ToggleKind Kind { get; set; }
    public RegistryValue ValueKey { get; set; } = new(RegistryHive.CurrentUser, string.Empty, string.Empty);
    public int OnValue { get; set; } = 1;
    public int OffValue { get; set; }

    /// <summary>
    /// KeyValues rows only: the string value whose non-null read means On.
    /// </summary>
    public RegistryValue? Probe { get; set; }

    /// <summary>
    /// KeyValues rows only: string writes applied when enabling.
    /// </summary>
    public List<RegistryStringWrite>? Writes { get; set; }

    /// <summary>
    /// KeyValues rows only: key trees deleted when disabling
    /// (<see cref="RegistryValue.Name"/> is ignored).
    /// </summary>
    public List<RegistryValue>? Deletes { get; set; }
}
