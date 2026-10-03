using Microsoft.Win32;
using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Reads, applies and reverts <see cref="RegistryTweak"/> definitions.
///
/// Reads are side-effect free and must never throw. Writes surface failures to the
/// caller so the UI can explain what went wrong instead of silently no-oping.
/// </summary>
public interface IRegistryTweakService
{
    /// <summary>True when the current process can write HKLM.</summary>
    bool IsElevated { get; }

    /// <summary>Reads the current state of a tweak without modifying anything.</summary>
    TweakState Read(RegistryTweak tweak);

    /// <summary>Writes the tweak's enabled value.</summary>
    Task ApplyAsync(RegistryTweak tweak, CancellationToken cancellationToken = default);

    /// <summary>Writes the tweak's disabled value.</summary>
    Task RevertAsync(RegistryTweak tweak, CancellationToken cancellationToken = default);

    /// <summary>Writes an explicit state (used by "revert to default").</summary>
    Task SetAsync(RegistryTweak tweak, TweakState state, CancellationToken cancellationToken = default);

    /// <summary>Restarts Explorer so shell tweaks take effect. Disruptive: confirm first.</summary>
    Task RestartExplorerAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads a string value from the registry. Returns null if not found.</summary>
    string? ReadString(RegistryHive hive, string keyPath, string valueName);

    /// <summary>Writes a string value to the registry.</summary>
    Task WriteString(RegistryHive hive, string keyPath, string valueName, string value);

    /// <summary>Reads a DWORD value from the registry. Returns null if not found.</summary>
    int? ReadDword(RegistryHive hive, string keyPath, string valueName);

    /// <summary>Writes a DWORD value to the registry.</summary>
    Task WriteDword(RegistryHive hive, string keyPath, string valueName, int value);
}
