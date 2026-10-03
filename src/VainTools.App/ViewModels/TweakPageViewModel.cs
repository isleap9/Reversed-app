using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// Shared view model for every registry-tweak page (Explorer, Context Menu, Visual,
/// System, General overview). Each page supplies its own tweak list; the behaviour —
/// read state, toggle, revert, restart Explorer, report errors — is identical.
/// </summary>
public partial class TweakPageViewModel : ViewModelBase
{
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<TweakPageViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    /// <summary>Rows for this page.</summary>
    public ObservableCollection<TweakToggleViewModel> Tweaks { get; } = [];

    public TweakPageViewModel(
        string title,
        IEnumerable<RegistryTweak> tweaks,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<TweakPageViewModel> logger)
    {
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;

        Title = title;
        IsElevated = registry.IsElevated;

        foreach (var tweak in tweaks)
        {
            var row = new TweakToggleViewModel(tweak, registry, logger);
            row.Changed += OnRowChanged;
            Tweaks.Add(row);
        }

        UpdateStatus();
    }

    /// <summary>True when any tweak on this page needs administrator rights.</summary>
    public bool HasAdminTweaks => Tweaks.Any(t => t.NeedsAdmin);

    /// <summary>True when any tweak on this page needs an Explorer restart.</summary>
    public bool HasExplorerTweaks => Tweaks.Any(t => t.NeedsExplorerRestart);

    /// <summary>True when an elevated helper is worth prompting for.</summary>
    public bool ShowElevationNotice => HasAdminTweaks && !IsElevated;

    /// <summary>Re-reads every tweak on the page from the registry.</summary>
    [RelayCommand]
    public void Refresh()
    {
        foreach (var row in Tweaks)
        {
            row.Refresh();
        }

        UpdateStatus();
    }

    /// <summary>
    /// Restarts Explorer so shell tweaks take effect. Confirmed first because it closes
    /// any open File Explorer windows.
    /// </summary>
    [RelayCommand]
    public async Task RestartExplorerAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Restart Explorer",
            "Explorer will be closed and restarted so the changes take effect. " +
            "Any open File Explorer windows will close.",
            confirmText: "Restart");

        if (!confirmed)
        {
            StatusMessage = "Explorer restart cancelled.";
            return;
        }

        try
        {
            await _registry.RestartExplorerAsync();
            StatusMessage = "Explorer restarted.";
            _infoBar.ShowSuccess("Explorer restarted", "Shell changes are now applied.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Explorer restart failed");
            StatusMessage = $"Could not restart Explorer: {ex.Message}";
            _infoBar.ShowError("Restart failed", ex.Message);
        }
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
        var on = Tweaks.Count(t => t.IsOn);
        StatusMessage = $"{on} of {Tweaks.Count} enabled";
    }
}
