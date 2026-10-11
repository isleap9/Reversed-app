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
    /// Optional second key for rows that must toggle together (e.g. the
    /// classic context menu row writes two CLSID InprocServer32 keys).
    /// When set, <see cref="Kind"/> must be <see cref="ToggleKind.KeyPresence"/>
    /// and both keys are created/deleted as a pair.
    /// </summary>
    public RegistryValue? SecondaryValueKey { get; set; }
}
