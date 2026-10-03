using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VainTools.Framework;
using VainTools.Framework.ViewModels;
using VainTools.Models;
using VainTools.Services;

namespace VainTools.App.ViewModels;

/// <summary>
/// ViewModel for the GPU Governor page.
/// Provides real-time GPU metrics with timer-based polling.
/// </summary>
public partial class GpuGovernorViewModel : ViewModelBase, IDisposable
{
    private readonly IGpuService _gpuService;
    private System.Timers.Timer? _monitoringTimer;

    [ObservableProperty]
    public partial bool IsMonitoring { get; set; }

    [ObservableProperty]
    public partial int Temperature { get; set; }

    [ObservableProperty]
    public partial int CoreClock { get; set; }

    [ObservableProperty]
    public partial int MemoryClock { get; set; }

    [ObservableProperty]
    public partial int Power { get; set; }

    [ObservableProperty]
    public partial int FanSpeed { get; set; }

    [ObservableProperty]
    public partial int Utilization { get; set; }

    [ObservableProperty]
    public partial GpuInfo? SelectedGpu { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsApplying { get; set; }

    [ObservableProperty]
    public partial int TargetFanSpeed { get; set; }

    [ObservableProperty]
    public partial int CoreClockOffset { get; set; }

    [ObservableProperty]
    public partial int MemoryClockOffset { get; set; }

    [ObservableProperty]
    public partial int TargetPowerLimit { get; set; }

    [ObservableProperty]
    public partial double PollingIntervalMs { get; set; } = 500;

    public ObservableCollection<GpuInfo> GpuList { get; } = new();
    public ObservableCollection<NvFanCurvePoint> FanCurvePoints { get; } = new();

    /// <summary>
    /// Initializes a new instance of the GpuGovernorViewModel.
    /// </summary>
    /// <param name="gpuService">The GPU service for reading metrics.</param>
    public GpuGovernorViewModel(IGpuService gpuService)
    {
        _gpuService = gpuService ?? throw new ArgumentNullException(nameof(gpuService));

        // Initialize GPU list
        _ = InitializeGpuListAsync();
    }

    partial void OnSelectedGpuChanged(GpuInfo? value)
    {
        if (value != null)
        {
            _ = RefreshMetricsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshMetricsAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing...";

            if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero)
            {
                Temperature = 0;
                CoreClock = 0;
                MemoryClock = 0;
                Power = 0;
                FanSpeed = 0;
                Utilization = 0;
                StatusMessage = "No GPU available";
                return;
            }

            var handle = SelectedGpu.Handle;

            var tempTask = _gpuService.GetTemperatureAsync(handle);
            var coreClockTask = _gpuService.GetCoreClockAsync(handle);
            var memClockTask = _gpuService.GetMemoryClockAsync(handle);
            var powerTask = _gpuService.GetPowerConsumptionAsync(handle);
            var fanTask = _gpuService.GetFanSpeedAsync(handle);
            var utilTask = _gpuService.GetUtilizationAsync(handle);

            await Task.WhenAll(tempTask, coreClockTask, memClockTask, powerTask, fanTask, utilTask);

            Temperature = tempTask.Result;
            CoreClock = coreClockTask.Result;
            MemoryClock = memClockTask.Result;
            Power = powerTask.Result;
            FanSpeed = fanTask.Result;
            Utilization = utilTask.Result;

            StatusMessage = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private bool CanRefresh() => !IsRefreshing;

    [RelayCommand(CanExecute = nameof(CanStartMonitoring))]
    private void StartMonitoring()
    {
        if (_monitoringTimer != null) return;

        _monitoringTimer = new System.Timers.Timer(PollingIntervalMs);
        _monitoringTimer.Elapsed += async (_, _) => await RefreshMetricsAsync();
        _monitoringTimer.AutoReset = true;
        _monitoringTimer.Start();
        IsMonitoring = true;
        StatusMessage = "Monitoring started";
    }

    private bool CanStartMonitoring() => !IsMonitoring;

    [RelayCommand(CanExecute = nameof(CanStopMonitoring))]
    private void StopMonitoring()
    {
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
        IsMonitoring = false;
        StatusMessage = "Monitoring stopped";
    }

    private bool CanStopMonitoring() => IsMonitoring;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task SetFanSpeedAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = $"Setting fan speed to {TargetFanSpeed}%...";
            await _gpuService.SetFanSpeedAsync(SelectedGpu.Handle, TargetFanSpeed);
            StatusMessage = $"Fan speed set to {TargetFanSpeed}%";
            await RefreshMetricsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set fan speed: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task SetCoreOffsetAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = $"Setting core clock offset to {CoreClockOffset} MHz...";
            await _gpuService.SetClockOffsetsAsync(SelectedGpu.Handle, CoreClockOffset, MemoryClockOffset);
            StatusMessage = $"Core clock offset set to {CoreClockOffset} MHz";
            await RefreshMetricsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set core offset: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task SetMemoryOffsetAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = $"Setting memory clock offset to {MemoryClockOffset} MHz...";
            await _gpuService.SetClockOffsetsAsync(SelectedGpu.Handle, CoreClockOffset, MemoryClockOffset);
            StatusMessage = $"Memory clock offset set to {MemoryClockOffset} MHz";
            await RefreshMetricsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set memory offset: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task SetPowerLimitAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = $"Setting power limit to {TargetPowerLimit}%...";
            await _gpuService.SetPowerLimitAsync(SelectedGpu.Handle, TargetPowerLimit);
            StatusMessage = $"Power limit set to {TargetPowerLimit}%";
            await RefreshMetricsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set power limit: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ResetToDefaultsAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Resetting to defaults...";
            // Reset all settings to default
            await _gpuService.SetClockOffsetsAsync(SelectedGpu.Handle, 0, 0);
            await _gpuService.SetPowerLimitAsync(SelectedGpu.Handle, 0);
            StatusMessage = "Reset to defaults complete";
            await RefreshMetricsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to reset: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApplyFanCurve))]
    private async Task ApplyFanCurveAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero || FanCurvePoints.Count == 0) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Applying fan curve...";
            await _gpuService.SetFanCurveAsync(SelectedGpu.Handle, FanCurvePoints.ToList());
            StatusMessage = "Fan curve applied";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to apply fan curve: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool CanApplyFanCurve() => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero && FanCurvePoints.Count > 0;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task LoadFanCurveAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Loading fan curve...";
            var points = await _gpuService.GetFanCurveAsync(SelectedGpu.Handle);
            FanCurvePoints.Clear();
            foreach (var point in points)
            {
                FanCurvePoints.Add(point);
            }
            StatusMessage = $"Loaded {points.Count} fan curve points";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load fan curve: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ResetAllAsync()
    {
        try
        {
            IsApplying = true;
            StatusMessage = "Resetting all settings...";
            TargetFanSpeed = 0;
            CoreClockOffset = 0;
            MemoryClockOffset = 0;
            TargetPowerLimit = 100;
            await RefreshMetricsAsync();
            StatusMessage = "All settings reset";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to reset: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool CanApply() => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero;

    private async Task InitializeGpuListAsync()
    {
        try
        {
            StatusMessage = "Detecting GPUs...";
            int count = await _gpuService.GetDeviceCountAsync();

            GpuList.Clear();

            if (count == 0)
            {
                GpuList.Add(new GpuInfo { Id = 0, Name = "No NVIDIA GPU detected", Handle = IntPtr.Zero });
                SelectedGpu = GpuList[0];
                StatusMessage = "No NVIDIA GPU found";
                return;
            }

            for (int i = 0; i < count; i++)
            {
                IntPtr handle = await _gpuService.GetDeviceHandleAsync(i);
                string name = await _gpuService.GetDeviceNameAsync(handle);

                GpuList.Add(new GpuInfo
                {
                    Id = i,
                    Name = name,
                    Handle = handle
                });
            }

            SelectedGpu = GpuList[0];
            StatusMessage = $"Found {count} GPU(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            GpuList.Add(new GpuInfo { Id = 0, Name = "Error detecting GPU", Handle = IntPtr.Zero });
            SelectedGpu = GpuList[0];
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
    }
}
