using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Startup page. Orchestrates Run-key entries and scheduled tasks.
/// </summary>
public partial class StartupViewModel : ViewModelBase
{
    private readonly IStartupService _startupService;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<StartupViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    /// <summary>Run key startup entries.</summary>
    public ObservableCollection<StartupEntry> RunKeyEntries { get; } = [];

    /// <summary>Scheduled task startup entries.</summary>
    public ObservableCollection<StartupEntry> ScheduledTasks { get; } = [];

    public StartupViewModel(
        IStartupService startupService,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<StartupViewModel> logger)
    {
        _startupService = startupService;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Startup";
        IsElevated = true; // Startup entries can be read without elevation
    }

    [RelayCommand]
    public void Refresh()
    {
        try
        {
            var runKeyEntries = _startupService.GetRunKeyEntries();
            RunKeyEntries.Clear();
            foreach (var entry in runKeyEntries)
            {
                RunKeyEntries.Add(entry);
            }

            var scheduledTasks = _startupService.GetScheduledTasks();
            ScheduledTasks.Clear();
            foreach (var task in scheduledTasks)
            {
                ScheduledTasks.Add(task);
            }

            StatusMessage = $"{RunKeyEntries.Count} Run key entries, {ScheduledTasks.Count} scheduled tasks";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load startup entries");
            StatusMessage = $"Could not load entries: {ex.Message}";
            _infoBar.ShowError("Load failed", ex.Message);
        }
    }

    [RelayCommand]
    public void ToggleRunKeyEntry(StartupEntry entry)
    {
        try
        {
            var newState = !entry.IsEnabled;
            _startupService.ToggleRunKeyEntry(entry, newState);
            StatusMessage = $"{(newState ? "Enabled" : "Disabled")} {entry.Name}";
            _infoBar.ShowSuccess("Entry updated", $"{entry.Name} has been {(newState ? "enabled" : "disabled")}.");
            Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle Run key entry");
            StatusMessage = $"Could not toggle entry: {ex.Message}";
            _infoBar.ShowError("Toggle failed", ex.Message);
        }
    }

    [RelayCommand]
    public void ToggleScheduledTask(StartupEntry entry)
    {
        try
        {
            var newState = !entry.IsEnabled;
            _startupService.ToggleScheduledTask(entry, newState);
            StatusMessage = $"{(newState ? "Enabled" : "Disabled")} {entry.Name}";
            Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle scheduled task");
            StatusMessage = $"Could not toggle task: {ex.Message}";
            _infoBar.ShowError("Toggle failed", ex.Message);
        }
    }
}
