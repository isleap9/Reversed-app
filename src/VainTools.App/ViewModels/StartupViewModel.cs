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
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<StartupViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Run key startup entries.</summary>
    public ObservableCollection<StartupEntry> RunKeyEntries { get; } = [];

    /// <summary>Scheduled task startup entries.</summary>
    public ObservableCollection<StartupEntry> ScheduledTasks { get; } = [];

    public StartupViewModel(
        IStartupService startupService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<StartupViewModel> logger)
    {
        _startupService = startupService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Startup";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var runKeyEntries = _startupService.GetRunKeyEntries();
            RunKeyEntries.Clear();
            foreach (var entry in runKeyEntries)
            {
                RunKeyEntries.Add(entry);
            }

            var scheduledTasks = await _startupService.GetScheduledTasksAsync();
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
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load entries: {ex.Message}";
            _infoBar.ShowError("Load failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleEntry))]
    public async Task ToggleRunKeyEntryAsync(StartupEntry entry)
    {
        try
        {
            var newState = !entry.IsEnabled;
            _startupService.ToggleRunKeyEntry(entry, newState);
            _infoBar.ShowSuccess("Entry updated", $"{entry.Name} has been {(newState ? "enabled" : "disabled")}.");
            await RefreshAsync();
            StatusMessage = $"{(newState ? "Enabled" : "Disabled")} {entry.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle Run key entry");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not toggle entry: {ex.Message}";
            _infoBar.ShowError("Toggle failed", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleEntry))]
    public async Task ToggleScheduledTaskAsync(StartupEntry entry)
    {
        try
        {
            var newState = !entry.IsEnabled;
            await _startupService.ToggleScheduledTaskAsync(entry, newState);
            _infoBar.ShowSuccess("Task updated", $"{entry.Name} has been {(newState ? "enabled" : "disabled")}.");
            await RefreshAsync();
            StatusMessage = $"{(newState ? "Enabled" : "Disabled")} {entry.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle scheduled task");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not toggle task: {ex.Message}";
            _infoBar.ShowError("Toggle failed", ex.Message);
        }
    }

    /// <summary>
    /// True when a toggle's new state differs from the model, i.e. the user really flipped it.
    /// A programmatic binding update produces an equal state and MUST be ignored: acting on it
    /// makes Refresh → rebind → Toggled → Toggle recurse until the process dies.
    /// </summary>
    public static bool IsUserToggle(StartupEntry entry, bool newIsOn) => entry.IsEnabled != newIsOn;

    [RelayCommand(CanExecute = nameof(CanToggleEntry))]
    public async Task DeleteRunKeyEntryAsync(StartupEntry entry)
    {
        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Delete Startup Entry",
                $"Delete \"{entry.Name}\" from {entry.Source}? This cannot be undone.",
                confirmText: "Delete");

            if (!confirmed)
            {
                StatusMessage = "Delete cancelled.";
                return;
            }

            _startupService.DeleteRunKeyEntry(entry);
            _infoBar.ShowSuccess("Entry deleted", $"{entry.Name} has been deleted.");
            await RefreshAsync();
            StatusMessage = $"Deleted {entry.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Run key entry");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not delete entry: {ex.Message}";
            _infoBar.ShowError("Delete failed", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleEntry))]
    public async Task DeleteScheduledTaskAsync(StartupEntry entry)
    {
        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Delete Scheduled Task",
                $"Delete task \"{entry.Name}\"? This cannot be undone.",
                confirmText: "Delete");

            if (!confirmed)
            {
                StatusMessage = "Delete cancelled.";
                return;
            }

            await _startupService.DeleteScheduledTaskAsync(entry);
            _infoBar.ShowSuccess("Task deleted", $"{entry.Name} has been deleted.");
            await RefreshAsync();
            StatusMessage = $"Deleted {entry.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete scheduled task");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not delete task: {ex.Message}";
            _infoBar.ShowError("Delete failed", ex.Message);
        }
    }

    private static bool CanToggleEntry(StartupEntry? entry) => entry is not null;
}
