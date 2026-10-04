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

    partial void OnSelectedProcessChanged(ProcessInfo? value) => ApplyAffinityCommand.NotifyCanExecuteChanged();
}

