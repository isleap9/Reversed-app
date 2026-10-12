namespace AkariTools.Framework.Models;

/// <summary>
/// One string value write for a <see cref="ToggleKind.KeyValues"/> row.
/// When <see cref="ExpandString"/> is true the value is stored as
/// REG_EXPAND_SZ (e.g. ShellNew ItemName under %systemroot%); otherwise
/// plain REG_SZ.
/// </summary>
public sealed record RegistryStringWrite(RegistryValue Value, string Data, bool ExpandString = false);
