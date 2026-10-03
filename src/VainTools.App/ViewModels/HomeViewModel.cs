using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.Messaging;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Home page: a machine summary plus quick actions that
/// navigate to the most-used feature pages.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly ISystemInfoService _systemInfo;
    private readonly IMessenger _messenger;
    private readonly ILogger<HomeViewModel> _logger;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial SystemInfo Info { get; set; } = SystemInfo.Unknown;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = App.AppVersion;

    /// <summary>Quick actions shown on the Home page.</summary>
    public ObservableCollection<QuickAction> QuickActions { get; } =
    [
        new("GPU", "GPU status, clocks and tuning", "\uE7F4", "VainTools.App.Features.Gpu.GpuPage"),
        new("Display", "Monitors, resolutions and EDID", "\uE7F8", "VainTools.App.Features.Gpu.Display.DisplayPage"),
        new("NVIDIA", "Driver settings (DRS)", "\uE9D9", "VainTools.App.Features.Gpu.Nvidia.Drs.DrsPage"),
        new("Performance", "Timer, MPO and scheduling", "\uE9D9", "VainTools.App.Features.Performance.PerformancePage"),
        new("Driver Manager", "Installed driver packages", "\uE7B8", "VainTools.App.Features.DriverManager.DriverManagerPage"),
        new("Device Cleaner", "Find orphaned devices and drivers", "\uE74D", "VainTools.App.Features.DeviceCleaner.DeviceCleanerPage"),
    ];

    public HomeViewModel(
        ISystemInfoService systemInfo,
        IMessenger messenger,
        ILogger<HomeViewModel> logger)
    {
        _systemInfo = systemInfo;
        _messenger = messenger;
        _logger = logger;

        Title = "Home";

        _ = LoadAsync();
    }

    /// <summary>Reloads the machine summary.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Reading system information...";
            Info = await _systemInfo.GetSystemInfoAsync();
            StatusMessage = $"Updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read system information");
            Info = SystemInfo.Unknown;
            StatusMessage = "Could not read system information.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates to a page by its fully-qualified type name, resolved against the
    /// assembly (WinUI has no x:Type markup extension, so targets are held as strings).
    /// </summary>
    [RelayCommand]
    public void NavigateTo(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        var pageType = typeof(HomeViewModel).Assembly.GetType(typeName);
        if (pageType is null)
        {
            _logger.LogWarning("Quick action target not found: {TypeName}", typeName);
            return;
        }

        _messenger.Send(new NavigationRequestedMessage(pageType));
    }
}

/// <summary>A quick-action tile on the Home page.</summary>
/// <param name="Label">Tile text.</param>
/// <param name="Description">Supporting line.</param>
/// <param name="Glyph">Segoe Fluent Icons glyph.</param>
/// <param name="TargetTypeName">Fully-qualified page type name.</param>
public sealed record QuickAction(string Label, string Description, string Glyph, string TargetTypeName);
