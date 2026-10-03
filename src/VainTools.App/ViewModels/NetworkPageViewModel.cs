using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Network page. Orchestrates adapter enumeration, DNS/NTP
/// server settings, and network offload toggles.
/// </summary>
public partial class NetworkPageViewModel : ViewModelBase
{
    private readonly INetworkService _networkService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<NetworkPageViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial string DnsServer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NtpServer { get; set; } = string.Empty;

    /// <summary>Network adapters found on the system.</summary>
    public ObservableCollection<NetworkAdapter> Adapters { get; } = [];

    /// <summary>Offload toggle rows backed by TweakCatalog.Network.</summary>
    public ObservableCollection<TweakToggleViewModel> OffloadTweaks { get; } = [];

    public NetworkPageViewModel(
        INetworkService networkService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<NetworkPageViewModel> logger)
    {
        _networkService = networkService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Network";
        IsElevated = registry.IsElevated;

        // Load offload tweaks from the catalog
        foreach (var tweak in TweakCatalog.Network)
        {
            var row = new TweakToggleViewModel(tweak, registry, logger);
            row.Changed += OnRowChanged;
            OffloadTweaks.Add(row);
        }
    }

    [RelayCommand]
    public async Task RefreshAdaptersAsync()
    {
        try
        {
            var adapters = await _networkService.GetAdaptersAsync();
            Adapters.Clear();
            foreach (var adapter in adapters)
            {
                Adapters.Add(adapter);
            }
            StatusMessage = $"{adapters.Count} adapters found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network adapters");
            StatusMessage = $"Could not get adapters: {ex.Message}";
            _infoBar.ShowError("Adapter enumeration failed", ex.Message);
        }
    }

    [RelayCommand]
    public async Task LoadDnsNtpAsync()
    {
        try
        {
            // Read DNS server from registry
            DnsServer = _registry.ReadString(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                "NameServer") ?? string.Empty;

            // Read NTP server from registry
            NtpServer = _registry.ReadString(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters",
                "NtpServer") ?? string.Empty;

            StatusMessage = "DNS/NTP settings loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DNS/NTP settings");
            StatusMessage = $"Could not load settings: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveDnsNtpAsync()
    {
        try
        {
            // Write DNS server
            await _registry.WriteString(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                "NameServer",
                DnsServer);

            // Write NTP server
            await _registry.WriteString(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters",
                "NtpServer",
                NtpServer);

            StatusMessage = "DNS/NTP settings saved";
            _infoBar.ShowSuccess("Settings saved", "DNS and NTP server settings have been updated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save DNS/NTP settings");
            StatusMessage = $"Could not save settings: {ex.Message}";
            _infoBar.ShowError("Save failed", ex.Message);
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        foreach (var row in OffloadTweaks)
        {
            row.Refresh();
        }
        _ = RefreshAdaptersAsync();
        _ = LoadDnsNtpAsync();
    }

    private void OnRowChanged(object? sender, EventArgs e)
    {
        if (sender is TweakToggleViewModel row && !string.IsNullOrEmpty(row.LastError))
        {
            _infoBar.ShowError($"Could not change \"{row.Name}\"", row.LastError!);
            row.LastError = null;
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var on = OffloadTweaks.Count(t => t.IsOn);
        StatusMessage = $"{on} of {OffloadTweaks.Count} offload tweaks enabled";
    }
}
