using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;

namespace VainTools.App.ViewModels;

/// <summary>
/// One toggle row on a tweak page. Wraps a <see cref="RegistryTweak"/> and keeps the
/// UI in step with the registry: the switch shows the *observed* state, and a failed
/// write rolls the switch back rather than leaving it lying about the machine.
/// </summary>
public partial class TweakToggleViewModel : ObservableObject
{
    private readonly IRegistryTweakService _registry;
    private readonly ILogger _logger;
    private bool _suppressWrite;

    [ObservableProperty]
    public partial bool IsOn { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StateText { get; set; } = string.Empty;

    public TweakToggleViewModel(RegistryTweak tweak, IRegistryTweakService registry, ILogger logger)
    {
        Tweak = tweak;
        _registry = registry;
        _logger = logger;

        Refresh();
    }

    /// <summary>The underlying tweak definition.</summary>
    public RegistryTweak Tweak { get; }

    public string Name => Tweak.Name;

    public string Description => Tweak.Description;

    /// <summary>True when the user needs to know this change needs administrator rights.</summary>
    public bool NeedsAdmin => Tweak.RequiresAdmin;

    /// <summary>True when the user needs to know Explorer will need a restart.</summary>
    public bool NeedsExplorerRestart => Tweak.RequiresExplorerRestart;

    /// <summary>Raised after a successful write so the page can update its summary.</summary>
    public event EventHandler? Changed;

    /// <summary>Re-reads the current state from the registry.</summary>
    public void Refresh()
    {
        var state = _registry.Read(Tweak);

        _suppressWrite = true;
        try
        {
            IsOn = state switch
            {
                TweakState.Enabled => true,
                TweakState.Disabled => false,
                // Fall back to the documented default so the switch shows a definite
                // position rather than guessing.
                _ => Tweak.DefaultWhenUnset == TweakState.Enabled,
            };
        }
        finally
        {
            _suppressWrite = false;
        }

        StateText = state switch
        {
            TweakState.Enabled => "On",
            TweakState.Disabled => "Off",
            TweakState.Unset => $"Not set (default: {(Tweak.DefaultWhenUnset == TweakState.Enabled ? "on" : "off")})",
            _ => "Unknown",
        };
    }

    partial void OnIsOnChanged(bool value)
    {
        if (_suppressWrite)
        {
            return;
        }

        _ = WriteAsync(value);
    }

    private async Task WriteAsync(bool enable)
    {
        var previous = IsOn;

        try
        {
            IsBusy = true;

            if (enable)
            {
                await _registry.ApplyAsync(Tweak);
            }
            else
            {
                await _registry.RevertAsync(Tweak);
            }

            Refresh();
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set tweak {Id}", Tweak.Id);

            // Roll the switch back so the UI never claims a change that did not happen.
            _suppressWrite = true;
            try
            {
                IsOn = !enable;
            }
            finally
            {
                _suppressWrite = false;
            }

            LastError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Most recent write failure, surfaced by the page.</summary>
    [ObservableProperty]
    public partial string? LastError { get; set; }
}
