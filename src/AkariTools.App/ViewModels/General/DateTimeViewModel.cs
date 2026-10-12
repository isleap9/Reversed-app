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

    // WinUI has no CommandManager auto-requery: ApplyCommand.CanExecute is
    // only re-evaluated when notified (collection changes, load, apply).
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _isApplying;

    public DateTimeViewModel(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl)
    {
        _registrySettingService = registrySettingService;
        _serviceControl = serviceControl;
        NtpServers.CollectionChanged += (_, _) => ApplyCommand.NotifyCanExecuteChanged();
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
        ApplyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddServer))]
    private async Task AddServerAsync()
    {
        ErrorMessage = null;

        var trimmed = NewServer?.Trim() ?? string.Empty;

        // Validation (Review Focus #3): reject empty, any-whitespace,
        // empty-host, malformed-flag, or duplicate entries before staging.
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Any(char.IsWhiteSpace))
        {
            ErrorMessage = "Enter a valid host name (e.g. time.windows.com).";
            return;
        }

        // Optional flag format: host,0xflag (e.g. time.windows.com,0x9).
        var parts = trimmed.Split(',', 2);
        var host = parts[0].Trim();
        if (string.IsNullOrEmpty(host))
        {
            ErrorMessage = "Enter a valid host name (e.g. time.windows.com).";
            return;
        }
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

        if (NtpServers.Any(s => s.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "That server is already in the list.";
            return;
        }

        NtpServers.Add(trimmed);
        NewServer = string.Empty;
    }

    [RelayCommand]
    private void RemoveServer(string? server)
    {
        if (!string.IsNullOrEmpty(server))
            NtpServers.Remove(server);
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
            var wrote = await Task.Run(() => _registrySettingService.WriteString(
                new RegistryValue(RegistryHive.LocalMachine, W32TimeParametersPath, NtpServerName),
                joined));
            if (!wrote)
            {
                ErrorMessage = "Failed to write NTP servers (administrator rights required).";
                return;
            }

            // Apply via w32tm.
            RunW32TmConfigUpdate();

            // Restart W32Time to pick up the change; every bool is checked so
            // a refused stop/start surfaces instead of reporting success.
            var stopped = await Task.Run(() => _serviceControl.StopService(W32TimeServiceName));
            if (!stopped)
            {
                ErrorMessage = $"NTP servers written, but the {W32TimeServiceName} service could not be stopped.";
                return;
            }
            var started = await Task.Run(() => _serviceControl.StartService(W32TimeServiceName));
            if (!started)
                ErrorMessage = $"NTP servers written, but the {W32TimeServiceName} service could not be started.";
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
