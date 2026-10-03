using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Sound page. Orchestrates audio device enumeration,
/// volume control, and audio enhancement toggles.
/// </summary>
public partial class SoundPageViewModel : ViewModelBase
{
    private readonly ISoundService _soundService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<SoundPageViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    /// <summary>Audio devices found on the system.</summary>
    public ObservableCollection<AudioDevice> Devices { get; } = [];

    /// <summary>Audio enhancement toggles backed by TweakCatalog.Sound.</summary>
    public ObservableCollection<TweakToggleViewModel> EnhancementTweaks { get; } = [];

    public SoundPageViewModel(
        ISoundService soundService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<SoundPageViewModel> logger)
    {
        _soundService = soundService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Sound";
        IsElevated = registry.IsElevated;

        // Load enhancement tweaks from the catalog
        foreach (var tweak in TweakCatalog.Sound)
        {
            var row = new TweakToggleViewModel(tweak, registry, logger);
            row.Changed += OnRowChanged;
            EnhancementTweaks.Add(row);
        }
    }

    [RelayCommand]
    public async Task RefreshDevicesAsync()
    {
        try
        {
            var devices = await _soundService.GetDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
            StatusMessage = $"{devices.Count} audio devices found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audio devices");
            StatusMessage = $"Could not get devices: {ex.Message}";
            _infoBar.ShowError("Device enumeration failed", ex.Message);
        }
    }

    public async Task SetVolumeAsync(string deviceId, float level)
    {
        try
        {
            await _soundService.SetVolumeAsync(deviceId, level);
            StatusMessage = $"Volume set to {level:P0}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set volume");
            _infoBar.ShowError("Volume change failed", ex.Message);
        }
    }

    public async Task SetMuteAsync(string deviceId, bool mute)
    {
        try
        {
            await _soundService.SetMuteAsync(deviceId, mute);
            StatusMessage = mute ? "Device muted" : "Device unmuted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set mute");
            _infoBar.ShowError("Mute change failed", ex.Message);
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        foreach (var row in EnhancementTweaks)
        {
            row.Refresh();
        }
        _ = RefreshDevicesAsync();
    }

    private void OnRowChanged(object? sender, EventArgs e)
    {
        if (sender is TweakToggleViewModel row && !string.IsNullOrEmpty(row.LastError))
        {
            _infoBar.ShowError($"Could not change \"{row.Name}\"", row.LastError!);
            row.LastError = null;
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var on = EnhancementTweaks.Count(t => t.IsOn);
        StatusMessage = $"{on} of {EnhancementTweaks.Count} enhancements enabled";
    }
}
