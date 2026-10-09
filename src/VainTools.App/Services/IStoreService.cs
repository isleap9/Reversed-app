namespace VainTools.App.Services;

/// <summary>
/// Searches for and installs applications through the winget CLI (D-08).
/// </summary>
public interface IStoreService
{
    /// <summary>
    /// Searches the winget sources for apps matching <paramref name="query"/>.
    /// A search that winget refuses (nonzero exit) or that cannot be parsed returns an
    /// empty list rather than throwing — the caller reports it (D-12 / T-06-14).
    /// </summary>
    IReadOnlyList<StoreApp> SearchApps(string query);

    /// <summary>
    /// Installs an app by package id. Requires elevation (D-10) and an explicit
    /// confirmation from the caller (D-11 / T-06-10). The raw result is returned so the
    /// caller decides what a failure means; process-layer exceptions are not swallowed.
    /// </summary>
    Task<ProcessResult> InstallAppAsync(string appId);
}
