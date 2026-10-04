using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Affinity page. Orchestrates process enumeration and CPU affinity management.
/// </summary>
public partial class AffinityViewModel : ViewModelBase
{
    private readonly IAffinityService _affinityService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<AffinityViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Running processes found on the system.</summary>
    public ObservableCollection<ProcessInfo> Processes { get; } = [];

    /// <summary>CPU affinity checkboxes for the selected process.</summary>
    public ObservableCollection<CpuAffinityViewModel> Cpus { get; } = [];

    /// <summary>Saved affinity rules (process name → mask).</summary>
    public ObservableCollection<AffinityRule> Rules { get; } = [];

    [ObservableProperty]
    public partial ProcessInfo? SelectedProcess { get; set; }

    public AffinityViewModel(
        IAffinityService affinityService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<AffinityViewModel> logger)
    {
        _affinityService = affinityService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Affinity";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public void Refresh()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var processes = _affinityService.GetProcesses();
            Processes.Clear();
            foreach (var process in processes)
            {
                Processes.Add(process);
            }

            ReloadRules();

            StatusMessage = $"{processes.Count} processes found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get processes");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not get processes: {ex.Message}";
            _infoBar.ShowError("Process enumeration failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectProcess))]
    public void SelectProcess(ProcessInfo process)
    {
        SelectedProcess = process;
        LoadAffinity(process.Id);
    }

    private bool CanSelectProcess(ProcessInfo? process) => process is not null;

    private void LoadAffinity(int processId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var mask = _affinityService.GetAffinityMask(processId);
            var cpuCount = _affinityService.GetCpuCount();
            Cpus.Clear();
            for (int i = 0; i < cpuCount; i++)
            {
                var isEnabled = (mask & (1UL << i)) != 0;
                Cpus.Add(new CpuAffinityViewModel(i, i, isEnabled));
            }
            StatusMessage = $"Loaded affinity for PID {processId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load affinity for PID {ProcessId}", processId);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load affinity: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApplyAffinity))]
    public void ApplyAffinity()
    {
        if (SelectedProcess is null)
        {
            return;
        }

        try
        {
            ErrorMessage = string.Empty;

            ulong mask = 0;
            foreach (var cpu in Cpus)
            {
                if (cpu.IsEnabled)
                {
                    mask |= 1UL << cpu.Index;
                }
            }

            _affinityService.SetAffinityMask(SelectedProcess.Id, mask);
            StatusMessage = $"Affinity set for {SelectedProcess.Name}";
            _infoBar.ShowSuccess("Affinity applied", $"CPU affinity has been updated for {SelectedProcess.Name}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set affinity");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not set affinity: {ex.Message}";
            _infoBar.ShowError("Affinity change failed", ex.Message);
        }
    }

    private bool CanApplyAffinity() => SelectedProcess is not null;

    [RelayCommand(CanExecute = nameof(CanApplyAffinity))]
    public void SaveRule()
    {
        if (SelectedProcess is null)
        {
            return;
        }

        try
        {
            ErrorMessage = string.Empty;

            var mask = BuildMaskFromCheckboxes();
            if (mask == 0)
            {
                ErrorMessage = "Select at least one CPU before saving a rule.";
                StatusMessage = "Could not save rule: no CPU selected.";
                return;
            }

            _affinityService.SaveRule(SelectedProcess.Name, mask);
            ReloadRules();
            StatusMessage = $"Rule saved for {SelectedProcess.Name}";
            _infoBar.ShowSuccess("Rule saved", $"Affinity rule for {SelectedProcess.Name} will be reapplied on demand.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save affinity rule");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not save rule: {ex.Message}";
            _infoBar.ShowError("Save failed", ex.Message);
        }
    }

    [RelayCommand]
    public async Task DeleteRuleAsync(AffinityRule rule)
    {
        if (rule is null)
        {
            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Delete Affinity Rule",
                $"Delete the saved rule for \"{rule.ProcessName}\"? This cannot be undone.",
                confirmText: "Delete");

            if (!confirmed)
            {
                StatusMessage = "Delete cancelled.";
                return;
            }

            _affinityService.DeleteRule(rule.ProcessName);
            ReloadRules();
            StatusMessage = $"Deleted rule for {rule.ProcessName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete affinity rule");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not delete rule: {ex.Message}";
            _infoBar.ShowError("Delete failed", ex.Message);
        }
    }

    [RelayCommand]
    public void ApplyRules()
    {
        try
        {
            ErrorMessage = string.Empty;

            var applied = _affinityService.ApplyRules();
            StatusMessage = applied == 0
                ? "No saved rules matched a running process"
                : $"Reapplied {applied} rule{(applied == 1 ? string.Empty : "s")}";
            _infoBar.ShowSuccess("Rules reapplied", StatusMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reapply affinity rules");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not reapply rules: {ex.Message}";
            _infoBar.ShowError("Reapply failed", ex.Message);
        }
    }

    private ulong BuildMaskFromCheckboxes()
    {
        ulong mask = 0;
        foreach (var cpu in Cpus)
        {
            if (cpu.IsEnabled)
            {
                mask |= 1UL << cpu.Index;
            }
        }

        return mask;
    }

    private void ReloadRules()
    {
        try
        {
            var rules = _affinityService.GetRules();
            Rules.Clear();
            foreach (var rule in rules)
            {
                Rules.Add(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load affinity rules");
        }
    }

    partial void OnSelectedProcessChanged(ProcessInfo? value)
    {
        ApplyAffinityCommand.NotifyCanExecuteChanged();
        SaveRuleCommand.NotifyCanExecuteChanged();
    }
}

