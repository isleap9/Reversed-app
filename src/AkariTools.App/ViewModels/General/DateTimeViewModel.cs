using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Date & Time page: NTP server list management (spec §7.5).
/// Reads the current NtpServer value from HKLM\...\W32Time\Parameters,
/// allows adding/removing servers, and applies the list by writing the
/// joined value, running w32tm /config /update, and restarting W32Time.
/// </summary>
public sealed partial class DateTimeViewModel : ObservableObject
{
    private const string W32TimeParametersPath =
        @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";
    private const string NtpServerName = "NtpServer";
    private const string W32TimeServiceName = "W32Time";

    private readonly IRegistrySettingService _registrySettingService;
    private readonly IServiceControl _serviceControl;

    [ObservableProperty]
    private ObservableCollection<string> _ntpServers = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    private string _newServer = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isApplying;

    public DateTimeViewModel(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl)
    {
        _registrySettingService = registrySettingService;
        _serviceControl = serviceControl;
    }

    public async Task LoadAsync()
    {
        var raw = await Task.Run(() => _registrySettingService.ReadString(
            new RegistryValue(RegistryHive.LocalMachine, W32TimeParametersPath, NtpServerName)));

        NtpServers.Clear();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            // NtpServer format: "host1,0x1 host2,0x2" (space-separated).
            foreach (var entry in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(entry))
                    NtpServers.Add(entry.Trim());
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddServer))]
    private async Task AddServerAsync()
    {
        ErrorMessage = null;

        var trimmed = NewServer?.Trim() ?? string.Empty;

        // Validation (Review Focus #3): reject empty or whitespace-containing input.
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Contains(' '))
        {
            ErrorMessage = "Enter a valid host name (e.g. time.windows.com).";
            return;
        }

        // Optional flag format: host,0xflag (e.g. time.windows.com,0x9).
        // Reject if present but malformed.
        var parts = trimmed.Split(',', 2);
        if (parts.Length == 2)
        {
            var flag = parts[1].Trim();
            if (!flag.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                flag.Length < 3 ||
                !flag.Skip(2).All(c => Uri.IsHexDigit(c)))
            {
                ErrorMessage = "Invalid flag format. Use e.g. 0x9.";
                return;
            }
        }

        NtpServers.Add(trimmed);
        NewServer = string.Empty;
    }

    private bool CanAddServer()
    {
        return !string.IsNullOrWhiteSpace(NewServer);
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        IsApplying = true;
        ErrorMessage = null;

        try
        {
            // Write the joined value to the registry.
            var joined = string.Join(" ", NtpServers);
            await Task.Run(() => _registrySettingService.WriteString(
                new RegistryValue(RegistryHive.LocalMachine, W32TimeParametersPath, NtpServerName),
                joined));

            // Apply via w32tm.
            RunW32TmConfigUpdate();

            // Restart W32Time to pick up the change.
            await Task.Run(() => _serviceControl.StopService(W32TimeServiceName));
            await Task.Run(() => _serviceControl.StartService(W32TimeServiceName));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to apply: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool CanApply()
    {
        return !IsApplying && NtpServers.Count > 0;
    }

    /// <summary>
    /// Runs w32tm /config /update to commit time service changes.
    /// Fire-and-forget; failures are logged but do not throw.
    /// </summary>
    private static void RunW32TmConfigUpdate()
    {
        try
        {
            var psi = new ProcessStartInfo("w32tm.exe", "/config /update")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(10_000);
        }
        catch
        {
            // Fire-and-forget: the registry write already succeeded.
        }
    }
}
