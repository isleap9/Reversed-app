using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Manual Windows Updates segmented row (design spec §5): index 1 = Enabled.
/// Backend is <c>HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\SetDisableUXWUAccess</c>
/// (DWORD), written inverted: <c>(index != 1) ? 1 : 0</c>. Writes
/// re-read to confirm; failures leave <see cref="SegmentedRowViewModel.SelectedIndex"/>
/// at its previous value and surface <see cref="LastError"/>. Never throws.
/// </summary>
public partial class ManualUpdatesViewModel : SegmentedRowViewModel
{
    private const string UpdatesPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
    private const string ValueName = "SetDisableUXWUAccess";

    private static readonly RegistryValue ValueKey =
        new(RegistryHive.LocalMachine, UpdatesPath, ValueName);

    private readonly IRegistrySettingService _registrySettingService;

    [ObservableProperty]
    private string? _lastError;

    public ManualUpdatesViewModel(IRegistrySettingService registrySettingService)
    {
        _registrySettingService = registrySettingService;
    }

    public string Label => "Manual Windows Updates";

    public string Description => "Let users check for and install updates manually.";

    /// <summary>Reads the current DWORD; 0 = Enabled (index 1), 1 or missing = Disabled (index 0).</summary>
    public async Task LoadAsync()
    {
        LastError = null;
        try
        {
            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(ValueKey)));
        }
        catch
        {
            // Never throw: leave SelectedIndex at its previous value on failure.
            LastError ??= $"Failed to read {ValueName}.";
        }
    }

    /// <summary>
    /// Writes the inverted DWORD for the requested index, then re-reads so the
    /// picker reflects reality. Failures keep the previous index and report.
    /// </summary>
    public override async Task ApplyAsync(int index)
    {
        if (index == SelectedIndex)
            return;

        LastError = null;
        try
        {
            var data = index != 1 ? 1 : 0;
            var ok = await Task.Run(() => _registrySettingService.WriteDword(ValueKey, data));
            if (!ok)
            {
                LastError = $"Failed to write {ValueName}.";
                return;
            }

            SelectedIndex = await Task.Run(() => Map(_registrySettingService.ReadDword(ValueKey)));
        }
        catch
        {
            // Never throw: SelectedIndex keeps its previous value on failure.
            LastError ??= "Failed to apply Manual Windows Updates setting.";
        }
    }

    private static int Map(int? value) => value == 0 ? 1 : 0;
}
