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
}
