using System.Threading.Tasks;

namespace VainTools.Services;

/// <summary>
/// Interface for system-level services.
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Enables performance mode optimizations.
    /// </summary>
    Task EnablePerformanceModeAsync();

    /// <summary>
    /// Disables performance mode optimizations.
    /// </summary>
    Task DisablePerformanceModeAsync();

    /// <summary>
    /// Gets the current performance mode status.
    /// </summary>
    Task<bool> IsPerformanceModeEnabledAsync();

    /// <summary>
    /// Enables game mode optimizations.
    /// </summary>
    Task EnableGameModeAsync();

    /// <summary>
    /// Disables game mode optimizations.
    /// </summary>
    Task DisableGameModeAsync();

    /// <summary>
    /// Gets the current game mode status.
    /// </summary>
    Task<bool> IsGameModeEnabledAsync();

    /// <summary>
    /// Applies all system tweaks from a profile.
    /// </summary>
    Task ApplySystemTweaksAsync();

    /// <summary>
    /// Reverts system tweaks to defaults.
    /// </summary>
    Task RevertSystemTweaksAsync();
}