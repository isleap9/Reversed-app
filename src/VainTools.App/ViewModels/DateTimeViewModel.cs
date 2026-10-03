using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Date &amp; Time page: the NTP server list Windows time sync uses,
/// and the installed time zones.
///
/// Backed by <c>HKLM\SYSTEM\CurrentControlSet\Services\W32Time\Parameters\NtpServer</c>,
/// the value the real app writes.
/// </summary>
public partial class DateTimeViewModel : ViewModelBase
{
    private const string W32TimeParameters = @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";
    private const string NtpValueName = "NtpServer";
    private const string TimeZonesKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones";

    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<DateTimeViewModel> _logger;

    [ObservableProperty]
    public partial string NtpServers { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string CurrentTimeZone { get; set; } = string.Empty;

    /// <summary>Time zones installed on this machine, from the registry.</summary>
    public ObservableCollection<string> TimeZones { get; } = [];

    /// <summary>Suggested public NTP servers.</summary>
    public ObservableCollection<string> SuggestedServers { get; } =
    [
        "time.windows.com,0x9",
        "pool.ntp.org,0x9",
        "time.google.com,0x9",
        "time.cloudflare.com,0x9",
    ];

    public DateTimeViewModel(
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<DateTimeViewModel> logger)
    {
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;

        Title = "Date & Time";

        CurrentTimeZone = TimeZoneInfo.Local.DisplayName;
        LoadNtpServers();
        LoadTimeZones();
    }

    /// <summary>Reads the configured NTP server list.</summary>
    [RelayCommand]
    public void LoadNtpServers()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(W32TimeParameters, writable: false);
            NtpServers = key?.GetValue(NtpValueName)?.ToString() ?? string.Empty;
            StatusMessage = string.IsNullOrWhiteSpace(NtpServers)
                ? "No NTP servers configured."
                : $"Current: {NtpServers}";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read NTP servers");
            NtpServers = string.Empty;
            StatusMessage = "Could not read the NTP configuration.";
        }
    }

    /// <summary>Writes the NTP server list. Requires administrator rights.</summary>
    [RelayCommand]
    public async Task ApplyNtpServersAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var value = NtpServers.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            StatusMessage = "Enter at least one NTP server.";
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Apply NTP servers",
            $"Windows time sync will use:{Environment.NewLine}{value}",
            confirmText: "Apply");

        if (!confirmed)
        {
            StatusMessage = "Cancelled.";
            return;
        }

        try
        {
            IsBusy = true;

            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey(W32TimeParameters, writable: true)
                    ?? throw new InvalidOperationException(
                        $"Could not open HKLM\\{W32TimeParameters}. Administrator rights are required.");

                key.SetValue(NtpValueName, value, RegistryValueKind.String);
            });

            StatusMessage = $"NTP servers set to {value}";
            _infoBar.ShowSuccess("NTP servers applied", "Run 'w32tm /resync' to resync immediately.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write NTP servers");
            StatusMessage = $"Could not apply: {ex.Message}";
            _infoBar.ShowError("Could not apply NTP servers", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadTimeZones()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TimeZonesKey, writable: false);
            if (key is null)
            {
                return;
            }

            foreach (var name in key.GetSubKeyNames())
            {
                using var zone = key.OpenSubKey(name);
                var display = zone?.GetValue("Display")?.ToString() ?? name;
                TimeZones.Add(display);
            }

            StatusMessage = $"Current time zone: {CurrentTimeZone}";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not enumerate time zones");
        }
    }
}
