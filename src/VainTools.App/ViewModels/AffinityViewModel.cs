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
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<AffinityViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    /// <summary>Running processes found on the system.</summary>
    public ObservableCollection<ProcessInfo> Processes { get; } = [];

    /// <summary>CPU affinity checkboxes for the selected process.</summary>
    public ObservableCollection<CpuAffinityViewModel> Cpus { get; } = [];

    [ObservableProperty]
    public partial ProcessInfo? SelectedProcess { get; set; }

    public AffinityViewModel(
        IAffinityService affinityService,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<AffinityViewModel> logger)
    {
        _affinityService = affinityService;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Affinity";
        IsElevated = affinityService is AffinityService ? true : false;
    }

    [RelayCommand]
    public void Refresh()
    {
        try
        {
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
            StatusMessage = $"Could not get processes: {ex.Message}";
            _infoBar.ShowError("Process enumeration failed", ex.Message);
        }
    }

    [RelayCommand]
    public void SelectProcess(ProcessInfo process)
    {
        SelectedProcess = process;
        LoadAffinity(process.Id);
    }

    private void LoadAffinity(int processId)
    {
        try
        {
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
            StatusMessage = $"Could not load affinity: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ApplyAffinity()
    {
        if (SelectedProcess is null) return;

        try
        {
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
            StatusMessage = $"Could not set affinity: {ex.Message}";
            _infoBar.ShowError("Affinity change failed", ex.Message);
        }
    }
}

/// <summary>
/// Represents a CPU affinity checkbox.
/// </summary>
public sealed partial class CpuAffinityViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial int CoreIndex { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    public CpuAffinityViewModel(int index, int coreIndex, bool isEnabled)
    {
        Index = index;
        CoreIndex = coreIndex;
        IsEnabled = isEnabled;
    }
}
