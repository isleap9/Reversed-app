using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ServiceProcess;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// General page: Hyper-V (bcdedit), Print Spooler and Remote Desktop toggles.
/// All toggles apply immediately (spec §12.7); failures set <see cref="LastError"/>
/// and leave the previous (reverted) value in place.
/// LPC row is intentionally absent from this slice — evidence names the handler
/// but not a verifiable backend; it returns with evidence in a later slice.
/// </summary>
public partial class GeneralViewModel : ObservableObject
{
    private const string HyperVSettingName = "hypervisorlaunchtype";
    private const string SpoolerServiceName = "Spooler";
    private const string TermServiceName = "TermService";
    private const string RdpFlagPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
    private const string RdpFlagName = "fDenyTSConnections";

    private readonly IServiceControl _serviceControl;
    private readonly IBcdService _bcdService;
    private readonly IRegistrySettingService _registrySettingService;

    [ObservableProperty]
    private bool _hyperVOn;

    [ObservableProperty]
    private bool _spoolerOn;

    /// <summary>Non-null when Spooler runs in Manual start mode (displays as On + note, spec §12.3).</summary>
    [ObservableProperty]
    private string? _spoolerNote;

    [ObservableProperty]
    private bool _rdpOn;

    [ObservableProperty]
    private string? _lastError;

    public GeneralViewModel(
        IServiceControl serviceControl,
        IBcdService bcdService,
        IRegistrySettingService registrySettingService)
    {
        _serviceControl = serviceControl;
        _bcdService = bcdService;
        _registrySettingService = registrySettingService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        LastError = null;

        // Hyper-V: parse hypervisorlaunchtype from bcdedit /enum; absent line = Off (spec §12.9).
        var hyperVValue = await Task.Run(() => _bcdService.ReadSetting(HyperVSettingName));
        HyperVOn = hyperVValue is not null
            && hyperVValue.Equals("auto", StringComparison.OrdinalIgnoreCase);

        // Spooler: Automatic or Manual displays On (Manual adds a note); Disabled = Off (spec §12.3).
        var spoolerMode = await Task.Run(() => _serviceControl.GetStartMode(SpoolerServiceName));
        SpoolerOn = spoolerMode is ServiceStartMode.Automatic or ServiceStartMode.Manual;
        SpoolerNote = spoolerMode == ServiceStartMode.Manual ? "Manual" : null;

        // RDP: fDenyTSConnections is the source of truth (0 = On); absent = Off (spec §12.1).
        var rdpFlag = await Task.Run(() => _registrySettingService.ReadDword(
            new RegistryValue(RegistryHive.LocalMachine, RdpFlagPath, RdpFlagName)));
        RdpOn = rdpFlag == 0;
    }

    [RelayCommand]
    public async Task SetHyperVAsync(bool on)
    {
        // Same-value guard: the view binds OneWay, so a user toggle reaches
        // here while HyperVOn holds the previous value; a re-fired toggle
        // carrying the shown value (revert echo) must not write.
        if (on == HyperVOn)
            return;

        LastError = null;
        var value = on ? "auto" : "off";
        var ok = await Task.Run(() => _bcdService.Set(HyperVSettingName, value));
        if (!ok)
        {
            // HyperVOn is unchanged, so force a notification: the OneWay-bound
            // switch still shows the user's flipped state and must snap back.
            LastError = $"Failed to set {HyperVSettingName} to '{value}'.";
            OnPropertyChanged(nameof(HyperVOn));
            return;
        }
        HyperVOn = on;
    }

    [RelayCommand]
    public async Task SetSpoolerAsync(bool on)
    {
        if (on == SpoolerOn)
            return;

        LastError = null;
        var mode = on ? ServiceStartMode.Automatic : ServiceStartMode.Disabled;
        var ok = await Task.Run(() => _serviceControl.SetStartMode(SpoolerServiceName, mode));
        if (!ok)
        {
            LastError = $"Failed to set {SpoolerServiceName} start mode to {mode}.";
            OnPropertyChanged(nameof(SpoolerOn));
            return;
        }

        // Immediate effect: start/stop the running service to match (spec §12.7).
        // SpoolerOn follows the start mode (spec §12.3), which was set, so the
        // switch stays even when the running state lags; LastError says so.
        var runningOk = on
            ? await Task.Run(() => _serviceControl.StartService(SpoolerServiceName))
            : await Task.Run(() => _serviceControl.StopService(SpoolerServiceName));
        if (!runningOk)
            LastError = $"{SpoolerServiceName} start mode set, but the service could not be {(on ? "started" : "stopped")}.";

        SpoolerOn = on;
        SpoolerNote = null;
    }

    [RelayCommand]
    public async Task SetRdpAsync(bool on)
    {
        if (on == RdpOn)
            return;

        LastError = null;
        var mode = on ? ServiceStartMode.Automatic : ServiceStartMode.Disabled;
        var flagValue = on ? 0 : 1;

        // The flag is the display source of truth (spec §12.1): if it fails, leave
        // the service untouched so machine state never disagrees with the switch.
        var flagOk = await Task.Run(() => _registrySettingService.WriteDword(
            new RegistryValue(RegistryHive.LocalMachine, RdpFlagPath, RdpFlagName), flagValue));
        if (!flagOk)
        {
            LastError = $"Failed to write {RdpFlagName}.";
            OnPropertyChanged(nameof(RdpOn));
            return;
        }

        var modeOk = await Task.Run(() => _serviceControl.SetStartMode(TermServiceName, mode));
        if (!modeOk)
        {
            // Roll the flag back: nothing else was applied, so restoring it
            // leaves the machine exactly as the (unchanged) switch shows.
            await Task.Run(() => _registrySettingService.WriteDword(
                new RegistryValue(RegistryHive.LocalMachine, RdpFlagPath, RdpFlagName), on ? 1 : 0));
            LastError = $"Failed to set {TermServiceName} start mode to {mode}.";
            OnPropertyChanged(nameof(RdpOn));
            return;
        }

        // Immediate effect: start/stop the running service to match (spec §12.7).
        // RdpOn follows the flag (spec §12.1), which was set, so the switch
        // stays even when the running state lags; LastError says so.
        var runningOk = on
            ? await Task.Run(() => _serviceControl.StartService(TermServiceName))
            : await Task.Run(() => _serviceControl.StopService(TermServiceName));
        if (!runningOk)
            LastError = $"Remote Desktop applied, but TermService could not be {(on ? "started" : "stopped")}.";

        RdpOn = on;
    }
}
