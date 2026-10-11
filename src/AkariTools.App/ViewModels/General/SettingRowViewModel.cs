using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    /// <summary>The row definition this view model was built from (pins key paths in tests).</summary>
    public ToggleSetting Setting => _setting;

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

    /// <summary>
    /// Applies the requested state. Bound to the control via the generated
    /// <see cref="SetCommand"/>; the same-value guard makes re-fired
    /// toggles (e.g. the switch reverting after a failed apply) no-ops.
    /// </summary>
    [RelayCommand]
    public async Task SetAsync(bool on)
    {
        // Guard: the page binds IsOn OneWay, so a user toggle reaches this
        // command while IsOn still holds the previous value. A re-fired
        // toggle carrying the value IsOn already shows (programmatic
        // revert) must not write.
        if (on == IsOn)
            return;

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

        // Force the re-read value onto the UI: IsOn only reaches the
        // OneWay-bound control through a change notification, and a failed
        // or no-op write re-reads to the *previous* value (no change). The
        // raise reverts the switch to the real machine state; the toggle
        // this re-fires is swallowed by the guard above.
        OnPropertyChanged(nameof(IsOn));
    }

    private bool ReadCurrent()
    {
        return _setting.Kind switch
        {
            // Dword: On when the stored value equals OnValue; absent (null) = Off.
            ToggleKind.Dword => _registrySettingService.ReadDword(_setting.ValueKey) == _setting.OnValue,
            // KeyPresence: On when the key exists. When SecondaryValueKey is
            // set, both keys must exist for the row to show On.
            ToggleKind.KeyPresence => KeyExistsWithSecondary(_setting.ValueKey),
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
            {
                _registrySettingService.CreateKey(_setting.ValueKey.Hive, _setting.ValueKey.Path);
                if (_setting.SecondaryValueKey is { } secondary)
                    _registrySettingService.CreateKey(secondary.Hive, secondary.Path);
            }
            else
            {
                _registrySettingService.DeleteKey(_setting.ValueKey.Hive, _setting.ValueKey.Path);
                if (_setting.SecondaryValueKey is { } secondary)
                    _registrySettingService.DeleteKey(secondary.Hive, secondary.Path);
            }
        }
    }

    private bool KeyExistsWithSecondary(RegistryValue key)
    {
        if (!_registrySettingService.KeyExists(key.Hive, key.Path))
            return false;
        if (_setting.SecondaryValueKey is { } secondary)
            return _registrySettingService.KeyExists(secondary.Hive, secondary.Path);
        return true;
    }
}
