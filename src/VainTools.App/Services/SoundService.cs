using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace VainTools.App.Services;

/// <summary>
/// Wraps WASAPI COM interop for audio device enumeration and volume control,
/// and registry access for spatial audio and enhancement settings.
/// </summary>
public sealed class SoundService : ISoundService
{
    private readonly IRegistryTweakService _registryTweakService;
    private readonly ILogger<SoundService> _logger;

    // Registry paths for audio settings
    private const string AudioRegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio";

    public SoundService(IRegistryTweakService registryTweakService, ILogger<SoundService> logger)
    {
        _registryTweakService = registryTweakService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<AudioDevice>> GetDevicesAsync()
    {
        return Task.Run(() =>
        {
            var devices = new List<AudioDevice>();

            try
            {
                // Create the MMDeviceEnumerator COM object
                var enumeratorType = Type.GetTypeFromCLSID(WasapiInterop.CLSID_MMDeviceEnumerator)
                    ?? throw new InvalidOperationException("MMDeviceEnumerator CLSID not found.");
                var enumeratorObj = Activator.CreateInstance(enumeratorType)
                    ?? throw new InvalidOperationException("Failed to create MMDeviceEnumerator instance.");
                var enumerator = (WasapiInterop.IMMDeviceEnumerator)enumeratorObj;

                // Get the default endpoint for comparison
                string? defaultDeviceId = null;
                try
                {
                    var hr = enumerator.GetDefaultAudioEndpoint(
                        WasapiInterop.EDataFlow.eRender, WasapiInterop.ERole.eMultimedia, out var defaultDevice);
                    if (hr >= 0 && defaultDevice != null)
                    {
                        defaultDevice.GetId(out defaultDeviceId);
                        Marshal.ReleaseComObject(defaultDevice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not get default audio endpoint");
                }

                // Enumerate active render devices
                var enumHr = enumerator.EnumAudioEndpoints(
                    WasapiInterop.EDataFlow.eRender, WasapiInterop.DeviceState.Active, out var deviceCollection);
                if (enumHr < 0 || deviceCollection == null)
                {
                    Marshal.ReleaseComObject(enumerator);
                    return devices;
                }

                deviceCollection.GetCount(out var count);
                for (int i = 0; i < count; i++)
                {
                    var itemHr = deviceCollection.Item(i, out var device);
                    if (itemHr < 0 || device == null)
                    {
                        continue;
                    }

                    try
                    {
                        device.GetId(out var id);
                        device.GetState(out var state);
                        var name = GetDeviceFriendlyName(device) ?? id;
                        var isDefault = string.Equals(id, defaultDeviceId, StringComparison.OrdinalIgnoreCase);

                        devices.Add(new AudioDevice(id, name, isDefault, state == WasapiInterop.DeviceState.Active));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error reading device at index {Index}", i);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(device);
                    }
                }

                Marshal.ReleaseComObject(deviceCollection);
                Marshal.ReleaseComObject(enumerator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate audio devices");
            }

            return devices;
        });
    }

    /// <inheritdoc />
    public Task<VolumeInfo> GetVolumeInfoAsync(string deviceId)
    {
        return Task.Run(() =>
        {
            try
            {
                var enumeratorType = Type.GetTypeFromCLSID(WasapiInterop.CLSID_MMDeviceEnumerator)
                    ?? throw new InvalidOperationException("MMDeviceEnumerator CLSID not found.");
                var enumeratorObj = Activator.CreateInstance(enumeratorType)
                    ?? throw new InvalidOperationException("Failed to create MMDeviceEnumerator instance.");
                var enumerator = (WasapiInterop.IMMDeviceEnumerator)enumeratorObj;

                var hr = enumerator.GetDevice(deviceId, out var device);
                if (hr < 0 || device == null)
                {
                    Marshal.ReleaseComObject(enumerator);
                    throw new InvalidOperationException($"Device '{deviceId}' not found.");
                }

                try
                {
                    var volume = ActivateVolumeInterface(device);
                    if (volume == null)
                    {
                        throw new InvalidOperationException("Failed to activate IAudioEndpointVolume.");
                    }

                    try
                    {
                        volume.GetMasterVolumeLevelScalar(out var level);
                        volume.GetMute(out var mute);
                        volume.GetVolumeRange(out var min, out var max, out _);

                        return new VolumeInfo(level, mute, min, max);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(volume);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                    Marshal.ReleaseComObject(enumerator);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get volume info for device {DeviceId}", deviceId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public Task SetVolumeAsync(string deviceId, float level)
    {
        return Task.Run(() =>
        {
            level = Math.Clamp(level, 0.0f, 1.0f);

            try
            {
                var enumeratorType = Type.GetTypeFromCLSID(WasapiInterop.CLSID_MMDeviceEnumerator)
                    ?? throw new InvalidOperationException("MMDeviceEnumerator CLSID not found.");
                var enumeratorObj = Activator.CreateInstance(enumeratorType)
                    ?? throw new InvalidOperationException("Failed to create MMDeviceEnumerator instance.");
                var enumerator = (WasapiInterop.IMMDeviceEnumerator)enumeratorObj;

                var hr = enumerator.GetDevice(deviceId, out var device);
                if (hr < 0 || device == null)
                {
                    Marshal.ReleaseComObject(enumerator);
                    throw new InvalidOperationException($"Device '{deviceId}' not found.");
                }

                try
                {
                    var volume = ActivateVolumeInterface(device);
                    if (volume == null)
                    {
                        throw new InvalidOperationException("Failed to activate IAudioEndpointVolume.");
                    }

                    try
                    {
                        Guid guid = Guid.Empty;
                        var setHr = volume.SetMasterVolumeLevelScalar(level, ref guid);
                        if (setHr < 0)
                        {
                            throw new InvalidOperationException($"SetMasterVolumeLevelScalar failed with HRESULT 0x{setHr:X8}");
                        }

                        _logger.LogInformation("Set volume for {DeviceId} to {Level:P0}", deviceId, level);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(volume);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                    Marshal.ReleaseComObject(enumerator);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set volume for device {DeviceId}", deviceId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public Task SetMuteAsync(string deviceId, bool mute)
    {
        return Task.Run(() =>
        {
            try
            {
                var enumeratorType = Type.GetTypeFromCLSID(WasapiInterop.CLSID_MMDeviceEnumerator)
                    ?? throw new InvalidOperationException("MMDeviceEnumerator CLSID not found.");
                var enumeratorObj = Activator.CreateInstance(enumeratorType)
                    ?? throw new InvalidOperationException("Failed to create MMDeviceEnumerator instance.");
                var enumerator = (WasapiInterop.IMMDeviceEnumerator)enumeratorObj;

                var hr = enumerator.GetDevice(deviceId, out var device);
                if (hr < 0 || device == null)
                {
                    Marshal.ReleaseComObject(enumerator);
                    throw new InvalidOperationException($"Device '{deviceId}' not found.");
                }

                try
                {
                    var volume = ActivateVolumeInterface(device);
                    if (volume == null)
                    {
                        throw new InvalidOperationException("Failed to activate IAudioEndpointVolume.");
                    }

                    try
                    {
                        Guid guid = Guid.Empty;
                        var setHr = volume.SetMute(mute, ref guid);
                        if (setHr < 0)
                        {
                            throw new InvalidOperationException($"SetMute failed with HRESULT 0x{setHr:X8}");
                        }

                        _logger.LogInformation("Set mute for {DeviceId} to {Mute}", deviceId, mute);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(volume);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                    Marshal.ReleaseComObject(enumerator);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set mute for device {DeviceId}", deviceId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public Task<SpatialAudioSettings> GetSpatialAudioSettingsAsync()
    {
        return Task.Run(() =>
        {
            var enabled = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "SpatialAudioEnabled") ?? 0;
            var type = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "SpatialAudioType") ?? 0;

            return new SpatialAudioSettings(enabled != 0, type);
        });
    }

    /// <inheritdoc />
    public async Task SetSpatialAudioSettingsAsync(SpatialAudioSettings settings)
    {
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "SpatialAudioEnabled", settings.Enabled ? 1 : 0);
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "SpatialAudioType", settings.Type);
    }

    /// <inheritdoc />
    public Task<AudioEnhancementSettings> GetEnhancementSettingsAsync()
    {
        return Task.Run(() =>
        {
            var enabled = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "AudioEnhancementsEnabled") ?? 1;
            var loudness = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "LoudnessEqualization") ?? 0;
            var bassBoost = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "BassBoost") ?? 0;
            var virtualSurround = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "VirtualSurround") ?? 0;
            var roomCorrection = _registryTweakService.ReadDword(RegistryHive.LocalMachine, AudioRegPath, "RoomCorrection") ?? 0;

            return new AudioEnhancementSettings(
                enabled != 0,
                loudness != 0,
                bassBoost != 0,
                virtualSurround != 0,
                roomCorrection != 0);
        });
    }

    /// <inheritdoc />
    public async Task SetEnhancementSettingsAsync(AudioEnhancementSettings settings)
    {
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "AudioEnhancementsEnabled", settings.Enabled ? 1 : 0);
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "LoudnessEqualization", settings.LoudnessEqualization ? 1 : 0);
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "BassBoost", settings.BassBoost ? 1 : 0);
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "VirtualSurround", settings.VirtualSurround ? 1 : 0);
        await _registryTweakService.WriteDword(
            RegistryHive.LocalMachine, AudioRegPath, "RoomCorrection", settings.RoomCorrection ? 1 : 0);
    }

    /// <summary>Activates the IAudioEndpointVolume interface on a device.</summary>
    private static WasapiInterop.IAudioEndpointVolume? ActivateVolumeInterface(WasapiInterop.IMMDevice device)
    {
        var iid = WasapiInterop.IID_IAudioEndpointVolume;
        var hr = device.Activate(
            ref iid,
            WasapiInterop.CLSCTX_ALL,
            IntPtr.Zero,
            out var obj);
        if (hr < 0 || obj == null)
        {
            return null;
        }

        return (WasapiInterop.IAudioEndpointVolume)obj;
    }

    /// <summary>Gets the friendly name of a device from its property store.</summary>
    private static string? GetDeviceFriendlyName(WasapiInterop.IMMDevice device)
    {
        try
        {
            var hr = device.OpenPropertyStore(WasapiInterop.STGM_READ, out var props);
            if (hr < 0 || props == null)
            {
                return null;
            }

            try
            {
                var key = new WasapiInterop.PropertyKey
                {
                    fmtid = WasapiInterop.PKEY_Device_FriendlyName,
                    pid = WasapiInterop.PKEY_Device_FriendlyName_Id,
                };

                var getHr = props.GetValue(ref key, out var var);
                if (getHr < 0)
                {
                    return null;
                }

                // PROPVARIANT with VT_LPWSTR (0x1F) stores a pointer to a string
                if (var.vt == 0x1F) // VT_LPWSTR
                {
                    var ptr = Marshal.ReadIntPtr(var.pointerValue);
                    var name = Marshal.PtrToStringUni(ptr);
                    Marshal.FreeCoTaskMem(ptr);
                    return name;
                }

                return null;
            }
            finally
            {
                Marshal.ReleaseComObject(props);
            }
        }
        catch
        {
            return null;
        }
    }
}
