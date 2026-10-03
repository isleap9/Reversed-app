using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Wraps PowerShell calls for network adapter enumeration and binding management.
/// </summary>
public sealed class NetworkService : INetworkService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<NetworkService> _logger;

    public NetworkService(IProcessRunner processRunner, ILogger<NetworkService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NetworkAdapter>> GetAdaptersAsync()
    {
        var result = await _processRunner.RunAsync("powershell.exe",
            "-NoProfile -Command \"Get-NetAdapter | Select-Object Name, InterfaceDescription, Status, MacAddress, LinkSpeed | ConvertTo-Json\"");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Get-NetAdapter failed (exit {result.ExitCode}): {result.StdErr}");
        return ParseAdapters(result.StdOut);
    }

    public async Task<bool> IsAdapterBindingEnabledAsync(string adapterName, string componentId)
    {
        var result = await _processRunner.RunAsync("powershell.exe",
            $"-NoProfile -Command \"Get-NetAdapterBinding -Name '{adapterName}' -ComponentID '{componentId}' | Select-Object -ExpandProperty Enabled\"");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Get-NetAdapterBinding failed (exit {result.ExitCode}): {result.StdErr}");
        return result.StdOut.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetAdapterBindingAsync(string adapterName, string componentId, bool enabled)
    {
        var verb = enabled ? "Enable" : "Disable";
        var result = await _processRunner.RunAsync("powershell.exe",
            $"-NoProfile -Command \"{verb}-NetAdapterBinding -Name '{adapterName}' -ComponentID '{componentId}'\"");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"{verb}-NetAdapterBinding failed (exit {result.ExitCode}): {result.StdErr}");
    }

    private static IReadOnlyList<NetworkAdapter> ParseAdapters(string json)
    {
        var adapters = new List<NetworkAdapter>();

        if (string.IsNullOrWhiteSpace(json))
            return adapters;

        // Handle both single object and array cases
        json = json.Trim();
        if (!json.StartsWith('['))
        {
            json = "[" + json + "]";
        }

        using var doc = JsonDocument.Parse(json);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var name = element.TryGetProperty("Name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
            var desc = element.TryGetProperty("InterfaceDescription", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty;
            var status = element.TryGetProperty("Status", out var statusEl) ? statusEl.GetString() ?? string.Empty : string.Empty;
            var mac = element.TryGetProperty("MacAddress", out var macEl) ? macEl.GetString() ?? string.Empty : string.Empty;
            var speed = element.TryGetProperty("LinkSpeed", out var speedEl) ? speedEl.GetString() ?? string.Empty : string.Empty;

            adapters.Add(new NetworkAdapter(name, desc, status, mac, speed));
        }

        return adapters;
    }
}
