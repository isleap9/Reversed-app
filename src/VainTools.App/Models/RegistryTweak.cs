using Microsoft.Win32;

namespace VainTools.App.Models;

/// <summary>
/// The observed state of a <see cref="RegistryTweak"/> on this machine.
/// </summary>
public enum TweakState
{
    /// <summary>Could not be determined (key missing, view mismatch, access denied).</summary>
    Unknown = 0,

    /// <summary>The value is present and matches the tweak's enabled value.</summary>
    Enabled,

    /// <summary>The value is present and matches the tweak's disabled value.</summary>
    Disabled,

    /// <summary>The value is absent, so the tweak sits at its documented default.</summary>
    Unset,
}

/// <summary>
/// A single registry-backed Windows tweak.
///
/// Every toggle in the General and System pages is one of these: a named value with
/// an "on" and an "off" representation, optionally needing administrator rights or an
/// Explorer restart to take effect.
/// </summary>
public sealed record RegistryTweak
{
    /// <summary>Stable identifier, e.g. "explorer.show-file-extensions".</summary>
    public required string Id { get; init; }

    /// <summary>Display name shown in the UI.</summary>
    public required string Name { get; init; }

    /// <summary>One-line explanation of what the tweak does.</summary>
    public string Description { get; init; } = string.Empty;

    public required RegistryHive Hive { get; init; }

    /// <summary>Key path below the hive, without a leading separator.</summary>
    public required string KeyPath { get; init; }

    public required string ValueName { get; init; }

    public RegistryValueKind ValueKind { get; init; } = RegistryValueKind.DWord;

    /// <summary>Value written when the tweak is turned on.</summary>
    public required object EnabledValue { get; init; }

    /// <summary>Value written when the tweak is turned off.</summary>
    public required object DisabledValue { get; init; }

    /// <summary>True when writing requires an elevated process.</summary>
    public bool RequiresAdmin { get; init; }

    /// <summary>True when Explorer must be restarted for the change to take effect.</summary>
    public bool RequiresExplorerRestart { get; init; }

    /// <summary>
    /// What "no value present" means for this tweak. Windows treats an absent value as
    /// a specific default, and showing that default beats showing "unknown".
    /// </summary>
    public TweakState DefaultWhenUnset { get; init; } = TweakState.Disabled;

    /// <summary>
    /// True for switches whose state is the existence of the key itself rather than the
    /// content of a value. Reverting such a tweak must delete the key; writing an empty
    /// value would leave the key present and the tweak would read back as enabled.
    /// </summary>
    public bool RemoveKeyWhenDisabled { get; init; }
}
