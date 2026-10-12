using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Push Notifications segmented row (design spec §5.1, <c>ev_bb40955</c>):
/// index 0 = Disabled. Disable writes the §5.1 set below (including
/// <c>WpnService\Start</c>=4, but only when the current <c>Start</c> query
/// returns nonzero, i.e. the service is present) and stops
/// <c>WpnService</c> for immediate effect; enable writes actives
/// (<c>Start</c>=2 unconditionally), deletes the three <c>NOC_*</c> values,
/// and starts <c>WpnService</c>. State maps from <c>ToastEnabled</c>: 0 =
/// Disabled, anything else or missing = Enabled. Sequential writes in spec
/// order, stop on first failure; failures leave
/// <see cref="SegmentedRowViewModel.SelectedIndex"/> at its previous value
/// and surface <see cref="LastError"/>. A failed start/stop keeps the applied
/// index (the registry set was written) and reports, mirroring
/// <see cref="PrintServiceViewModel"/>. Never throws.
/// </summary>
public partial class PushNotificationsViewModel : SegmentedRowViewModel
{
    private const string ServiceName = "WpnService";

    private const string WpnServicePath = @"SYSTEM\CurrentControlSet\Services\WpnService";
    private const string ConsentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener";
    private const string PushNotificationsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string PushPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string ExplorerAdvancedPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string NotificationSettingsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";

    private static readonly RegistryValue StartKey =
        new(RegistryHive.LocalMachine, WpnServicePath, "Start");

    private static readonly RegistryValue ConsentKey =
        new(RegistryHive.CurrentUser, ConsentStorePath, "Value");

    private static readonly RegistryValue ToastKey =
        new(RegistryHive.LocalMachine, PushNotificationsPath, "ToastEnabled");

    private static readonly RegistryValue NoCloudKey =
        new(RegistryHive.LocalMachine, PushPolicyPath, "NoCloudApplicationNotification");

    private static readonly RegistryValue ShowSyncKey =
        new(RegistryHive.CurrentUser, ExplorerAdvancedPath, "ShowSyncProviderNotifications");

    private static readonly RegistryValue[] NocKeys =
    [
        new(RegistryHive.CurrentUser, NotificationSettingsPath, "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND"),
        new(RegistryHive.CurrentUser, NotificationSettingsPath, "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK"),
        new(RegistryHive.CurrentUser, NotificationSettingsPath, "NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK"),
    ];

    private readonly IRegistrySettingService _registrySettingService;
    private readonly IServiceControl _serviceControl;

    [ObservableProperty]
    private string? _lastError;

    public PushNotificationsViewModel(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl)
    {
        _registrySettingService = registrySettingService;
        _serviceControl = serviceControl;
    }

    public string Label => "Push Notifications";

    public string Description => "Turn Windows push notifications on or off.";

    /// <summary>Reads ToastEnabled; 0 = Disabled (index 0), else Enabled (index 1).</summary>
    public async Task LoadAsync()
    {
        LastError = null;
        try
        {
            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(ToastKey)));
        }
        catch
        {
            // Never throw: leave SelectedIndex at its previous value on failure.
            LastError ??= "Failed to read ToastEnabled.";
        }
    }

    /// <summary>
    /// Writes the disable set (or the enable actives + NOC_* deletions) in
    /// spec order, then starts/stops WpnService and re-reads so the picker
    /// reflects reality. The first failed registry write keeps the previous
    /// index and reports; later values are skipped.
    /// </summary>
    public override async Task ApplyAsync(int index)
    {
        if (index == SelectedIndex)
            return;

        LastError = null;
        try
        {
            var ok = index == 0
                ? await Task.Run(WriteDisableSet)
                : await Task.Run(WriteEnableSet);
            if (!ok)
            {
                LastError = index == 0
                    ? "Failed to write Push Notifications setting."
                    : "Failed to delete Push Notifications setting.";
                return;
            }

            var runningOk = index == 0
                ? await Task.Run(() => _serviceControl.StopService(ServiceName))
                : await Task.Run(() => _serviceControl.StartService(ServiceName));
            if (!runningOk)
                LastError = $"{ServiceName} setting applied, but the service could not be {(index == 0 ? "stopped" : "started")}.";

            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(ToastKey)));
        }
        catch
        {
            // Never throw: SelectedIndex keeps its previous value on failure.
            LastError ??= "Failed to apply Push Notifications setting.";
        }
    }

    private bool WriteDisableSet()
    {
        // Only touch Start when the service is present (current Start nonzero).
        if (_registrySettingService.ReadDword(StartKey) is int current && current != 0)
        {
            if (!_registrySettingService.WriteDword(StartKey, 4))
                return false;
        }

        if (!_registrySettingService.WriteString(ConsentKey, "Deny"))
            return false;
        if (!_registrySettingService.WriteDword(ToastKey, 0))
            return false;
        if (!_registrySettingService.WriteDword(NoCloudKey, 1))
            return false;
        if (!_registrySettingService.WriteDword(ShowSyncKey, 0))
            return false;

        foreach (var key in NocKeys)
        {
            if (!_registrySettingService.WriteDword(key, 0))
                return false;
        }

        return true;
    }

    private bool WriteEnableSet()
    {
        if (!_registrySettingService.WriteDword(StartKey, 2))
            return false;
        if (!_registrySettingService.WriteString(ConsentKey, "Allow"))
            return false;
        if (!_registrySettingService.WriteDword(ToastKey, 1))
            return false;
        if (!_registrySettingService.WriteDword(NoCloudKey, 0))
            return false;
        if (!_registrySettingService.WriteDword(ShowSyncKey, 1))
            return false;

        foreach (var key in NocKeys)
        {
            if (!_registrySettingService.DeleteValue(key))
                return false;
        }

        return true;
    }

    private static int Map(int? value) => value == 0 ? 0 : 1;
}
