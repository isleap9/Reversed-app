using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Game Bar segmented row (design spec §5.1, <c>ev_c44a2bd</c>): index 0 =
/// Disabled. Disable writes the 8-value set below (7 DWORDs + the 3
/// <c>HKCR\ms-gamebar</c> strings); enable deletes each value. State maps
/// from the <c>AllowGameDVR</c> policy DWORD: 0 = Disabled, anything else or
/// missing = Enabled. Sequential writes in spec order, stop on first
/// failure; failures leave <see cref="SegmentedRowViewModel.SelectedIndex"/>
/// at its previous value and surface <see cref="LastError"/>. Never throws.
/// </summary>
public partial class GameBarViewModel : SegmentedRowViewModel
{
    private const string GameBarPath = @"SOFTWARE\Microsoft\GameBar";
    private const string GameDvrPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string GameDvrPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
    private const string GameDvrPolicyManagerPath = @"SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR";
    private const string PresenceWriterPath = @"SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter";
    private const string MsGamebarPath = "ms-gamebar";

    // Disable-write set in spec §5.1 order (ev_c44a2bd). The ms-gamebar
    // default value uses the empty name; the rest are DWORDs.
    private static readonly (RegistryValue Key, int Data)[] DisableDwords =
    [
        new(new(RegistryHive.CurrentUser, GameBarPath, "GamePanelStartupTipIndex"), 3),
        new(new(RegistryHive.CurrentUser, GameBarPath, "ShowStartupPanel"), 0),
        new(new(RegistryHive.CurrentUser, GameBarPath, "UseNexusForGameBarEnabled"), 0),
        new(new(RegistryHive.CurrentUser, GameDvrPath, "AppCaptureEnabled"), 0),
        new(new(RegistryHive.LocalMachine, GameDvrPolicyPath, "AllowGameDVR"), 0),
        new(new(RegistryHive.LocalMachine, GameDvrPolicyManagerPath, "value"), 0),
        new(new(RegistryHive.LocalMachine, PresenceWriterPath, "ActivationType"), 0),
    ];

    private static readonly (RegistryValue Key, string Data)[] DisableStrings =
    [
        new(new(RegistryHive.ClassesRoot, MsGamebarPath, string.Empty), "URL:ms-gamebar"),
        new(new(RegistryHive.ClassesRoot, MsGamebarPath, "URL Protocol"), string.Empty),
        new(new(RegistryHive.ClassesRoot, MsGamebarPath, "NoOpenWith"), string.Empty),
    ];

    private static readonly RegistryValue PolicyKey =
        new(RegistryHive.LocalMachine, GameDvrPolicyPath, "AllowGameDVR");

    private readonly IRegistrySettingService _registrySettingService;

    [ObservableProperty]
    private string? _lastError;

    public GameBarViewModel(IRegistrySettingService registrySettingService)
    {
        _registrySettingService = registrySettingService;
    }

    public string Label => "Game Bar";

    public string Description => "Turn the Game Bar and Game DVR features on or off.";

    /// <summary>Reads the AllowGameDVR policy DWORD; 0 = Disabled (index 0), else Enabled (index 1).</summary>
    public async Task LoadAsync()
    {
        LastError = null;
        try
        {
            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(PolicyKey)));
        }
        catch
        {
            // Never throw: leave SelectedIndex at its previous value on failure.
            LastError ??= "Failed to read AllowGameDVR.";
        }
    }

    /// <summary>
    /// Writes the disable set (or deletes each value for enable) in spec
    /// order, then re-reads so the picker reflects reality. The first failed
    /// write keeps the previous index and reports; later values are skipped.
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
                : await Task.Run(DeleteAllValues);
            if (!ok)
            {
                LastError = index == 0
                    ? "Failed to write Game Bar setting."
                    : "Failed to delete Game Bar setting.";
                return;
            }

            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(PolicyKey)));
        }
        catch
        {
            // Never throw: SelectedIndex keeps its previous value on failure.
            LastError ??= "Failed to apply Game Bar setting.";
        }
    }

    private bool WriteDisableSet()
    {
        foreach (var (key, data) in DisableDwords)
        {
            if (!_registrySettingService.WriteDword(key, data))
                return false;
        }

        foreach (var (key, data) in DisableStrings)
        {
            if (!_registrySettingService.WriteString(key, data))
                return false;
        }

        return true;
    }

    private bool DeleteAllValues()
    {
        foreach (var (key, _) in DisableDwords)
        {
            if (!_registrySettingService.DeleteValue(key))
                return false;
        }

        foreach (var (key, _) in DisableStrings)
        {
            if (!_registrySettingService.DeleteValue(key))
                return false;
        }

        return true;
    }

    private static int Map(int? value) => value == 0 ? 0 : 1;
}
