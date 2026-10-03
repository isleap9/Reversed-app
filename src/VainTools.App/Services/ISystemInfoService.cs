using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Reads machine information for the Home page.
/// Implementations must never throw: an unavailable source yields the
/// corresponding "Unknown …" placeholder.
/// </summary>
public interface ISystemInfoService
{
    /// <summary>Collects a fresh snapshot of machine information.</summary>
    /// <param name="cancellationToken">Cancels the underlying queries.</param>
    Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default);
}
