namespace VainTools.App.Services;

/// <summary>
/// Enumerates network adapters and manages adapter bindings via PowerShell.
/// </summary>
public interface INetworkService
{
    /// <summary>Gets all network adapters with their status and properties.</summary>
    Task<IReadOnlyList<NetworkAdapter>> GetAdaptersAsync();

    /// <summary>Checks whether a specific adapter binding is enabled.</summary>
    Task<bool> IsAdapterBindingEnabledAsync(string adapterName, string componentId);

    /// <summary>Enables or disables an adapter binding.</summary>
    Task SetAdapterBindingAsync(string adapterName, string componentId, bool enabled);
}
