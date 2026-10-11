using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// ViewModel for one <see cref="ToggleSetting"/> row, consumed by the shared
/// <c>ToggleSettingRow</c> control (Explorer/Visual/ContextMenu pages).
/// <see cref="LoadAsync"/> reads the current machine state;
/// <see cref="SetAsync"/> writes the requested state and then re-reads, so the
/// switch always reflects reality (Review Focus #5). Never throws.
/// </summary>
public partial class SettingRowViewModel : ObservableObject
{
    private readonly ToggleSetting _setting;
    private readonly IRegistrySettingService _registrySettingService;

    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private bool _isApplying;

    public SettingRowViewModel(ToggleSetting setting, IRegistrySettingService registrySettingService)
    {
        _setting = setting;
        _registrySettingService = registrySettingService;
        _label = setting.Label;
        _description = setting.Description;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsOn = await Task.Run(() => ReadCurrent());
        }
        catch
        {
            // Never throw: leave IsOn at its previous value on failure.
        }
    }

    public async Task SetAsync(bool on)
    {
        IsApplying = true;
        try
        {
            await Task.Run(() => WriteState(on));

            // Re-read after write: the switch reflects the real machine state,
            // not the requested one (Review Focus #5).
            IsOn = await Task.Run(() => ReadCurrent());
        }
        catch
        {
            // Never throw: IsOn keeps its previous value when a write or re-read fails.
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool ReadCurrent()
    {
        return _setting.Kind switch
        {
            // Dword: On when the stored value equals OnValue; absent (null) = Off.
            ToggleKind.Dword => _registrySettingService.ReadDword(_setting.ValueKey) == _setting.OnValue,
            // KeyPresence: On when the key exists.
            ToggleKind.KeyPresence => _registrySettingService.KeyExists(_setting.ValueKey.Hive, _setting.ValueKey.Path),
            _ => false
        };
    }

    private void WriteState(bool on)
    {
        if (_setting.Kind == ToggleKind.Dword)
        {
            _registrySettingService.WriteDword(_setting.ValueKey, on ? _setting.OnValue : _setting.OffValue);
        }
        else
        {
            if (on)
                _registrySettingService.CreateKey(_setting.ValueKey.Hive, _setting.ValueKey.Path);
            else
                _registrySettingService.DeleteKey(_setting.ValueKey.Hive, _setting.ValueKey.Path);
        }
    }
}
