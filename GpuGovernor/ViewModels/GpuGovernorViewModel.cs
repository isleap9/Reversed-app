using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using VainTools.Core;
using VainTools.Models;
using VainTools.Services;

namespace VainTools.GpuGovernor.ViewModels;

/// <summary>
/// ViewModel for the GPU Governor page.
/// Provides real-time GPU metrics with timer-based polling.
/// </summary>
public class GpuGovernorViewModel : ObservableObject, IDisposable
{
    private readonly IGpuService _gpuService;
    private System.Timers.Timer? _monitoringTimer;
    private bool _isMonitoring;
    private int _temperature;
    private int _coreClock;
    private int _memoryClock;
    private int _power;
    private int _fanSpeed;
    private int _utilization;
    private GpuInfo? _selectedGpu;
    private string _statusMessage = "Ready";
    private bool _isRefreshing;
    private bool _isApplying;
    private int _targetFanSpeed;
    private int _coreClockOffset;
    private int _memoryClockOffset;
    private int _targetPowerLimit;

    /// <summary>
    /// Initializes a new instance of the GpuGovernorViewModel.
    /// </summary>
    /// <param name="gpuService">The GPU service for reading metrics.</param>
    public GpuGovernorViewModel(IGpuService gpuService)
    {
        _gpuService = gpuService ?? throw new ArgumentNullException(nameof(gpuService));

        GpuList = new ObservableCollection<GpuInfo>();
        FanCurvePoints = new ObservableCollection<NvFanCurvePoint>();

        RefreshCommand = new RelayCommand(async () => await RefreshMetricsAsync(), () => !IsRefreshing);
        StartMonitoringCommand = new RelayCommand(StartMonitoring, () => !IsMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);

        // Control commands
        SetFanSpeedCommand = new RelayCommand(async () => await SetFanSpeedAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        SetCoreOffsetCommand = new RelayCommand(async () => await SetCoreOffsetAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        SetMemoryOffsetCommand = new RelayCommand(async () => await SetMemoryOffsetAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        SetPowerLimitCommand = new RelayCommand(async () => await SetPowerLimitAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        ResetToDefaultsCommand = new RelayCommand(async () => await ResetToDefaultsAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        ApplyFanCurveCommand = new RelayCommand(async () => await ApplyFanCurveAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero && FanCurvePoints.Count > 0);
        LoadFanCurveCommand = new RelayCommand(async () => await LoadFanCurveAsync(), () => !IsApplying && SelectedGpu?.Handle != IntPtr.Zero);
        ResetCommand = new RelayCommand(async () => await ResetAllAsync(), () => !IsApplying);

        // Initialize GPU list
        _ = InitializeGpuListAsync();
    }

    #region Properties

    /// <summary>
    /// Current GPU temperature in degrees Celsius.
    /// </summary>
    public int Temperature
    {
        get => _temperature;
        private set => SetProperty(ref _temperature, value);
    }

    /// <summary>
    /// Current core clock speed in MHz.
    /// </summary>
    public int CoreClock
    {
        get => _coreClock;
        private set => SetProperty(ref _coreClock, value);
    }

    /// <summary>
    /// Current memory clock speed in MHz.
    /// </summary>
    public int MemoryClock
    {
        get => _memoryClock;
        private set => SetProperty(ref _memoryClock, value);
    }

    /// <summary>
    /// Current power consumption in watts.
    /// </summary>
    public int Power
    {
        get => _power;
        private set => SetProperty(ref _power, value);
    }

    /// <summary>
    /// Current fan speed percentage.
    /// </summary>
    public int FanSpeed
    {
        get => _fanSpeed;
        private set => SetProperty(ref _fanSpeed, value);
    }

    /// <summary>
    /// Current GPU utilization percentage.
    /// </summary>
    public int Utilization
    {
        get => _utilization;
        private set => SetProperty(ref _utilization, value);
    }

    /// <summary>
    /// List of available GPUs in the system.
    /// </summary>
    public ObservableCollection<GpuInfo> GpuList { get; }

    /// <summary>
    /// Currently selected GPU.
    /// </summary>
    public GpuInfo? SelectedGpu
    {
        get => _selectedGpu;
        set
        {
            if (SetProperty(ref _selectedGpu, value))
            {
                // Refresh metrics when GPU selection changes
                _ = RefreshMetricsAsync();
            }
        }
    }

    /// <summary>
    /// Status message for the UI.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Whether monitoring is currently active.
    /// </summary>
    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                // Raise CanExecuteChanged for monitoring commands
                (StartMonitoringCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StopMonitoringCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Whether a refresh operation is in progress.
    /// </summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// The polling interval in milliseconds.
    /// </summary>
    public double PollingIntervalMs { get; set; } = 500;

    /// <summary>
    /// Whether a control apply operation is in progress.
    /// Disables controls during apply operations.
    /// </summary>
    public bool IsApplying
    {
        get => _isApplying;
        private set
        {
            if (SetProperty(ref _isApplying, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Target fan speed percentage (0-100).
    /// </summary>
    public int TargetFanSpeed
    {
        get => _targetFanSpeed;
        set
        {
            if (SetProperty(ref _targetFanSpeed, value))
            {
                (SetFanSpeedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Core clock offset in MHz (-500 to +500).
    /// </summary>
    public int CoreClockOffset
    {
        get => _coreClockOffset;
        set
        {
            if (SetProperty(ref _coreClockOffset, value))
            {
                (SetCoreOffsetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Memory clock offset in MHz (-500 to +500).
    /// </summary>
    public int MemoryClockOffset
    {
        get => _memoryClockOffset;
        set
        {
            if (SetProperty(ref _memoryClockOffset, value))
            {
                (SetMemoryOffsetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Target power limit as percentage of default (50-150).
    /// </summary>
    public int TargetPowerLimit
    {
        get => _targetPowerLimit;
        set
        {
            if (SetProperty(ref _targetPowerLimit, value))
            {
                (SetPowerLimitCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Fan curve points for the fan curve editor.
    /// </summary>
    public ObservableCollection<NvFanCurvePoint> FanCurvePoints { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh metrics immediately.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Command to start real-time monitoring.
    /// </summary>
    public ICommand StartMonitoringCommand { get; }

    /// <summary>
    /// Command to stop real-time monitoring.
    /// </summary>
    public ICommand StopMonitoringCommand { get; }

    /// <summary>
    /// Command to set the fan speed to TargetFanSpeed.
    /// </summary>
    public ICommand SetFanSpeedCommand { get; }

    /// <summary>
    /// Command to set the core clock offset.
    /// </summary>
    public ICommand SetCoreOffsetCommand { get; }

    /// <summary>
    /// Command to set the memory clock offset.
    /// </summary>
    public ICommand SetMemoryOffsetCommand { get; }

    /// <summary>
    /// Command to set the power limit.
    /// </summary>
    public ICommand SetPowerLimitCommand { get; }

    /// <summary>
    /// Command to reset all settings to defaults.
    /// </summary>
    public ICommand ResetToDefaultsCommand { get; }

    /// <summary>
    /// Command to apply the current fan curve to the GPU.
    /// </summary>
    public ICommand ApplyFanCurveCommand { get; }

    /// <summary>
    /// Command to load the current fan curve from the GPU.
    /// </summary>
    public ICommand LoadFanCurveCommand { get; }

    /// <summary>
    /// Command to reset all offsets to 0 and fan speed to auto.
    /// </summary>
    public ICommand ResetCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Initializes the GPU list by querying available devices.
    /// </summary>
    private async Task InitializeGpuListAsync()
    {
        try
        {
            StatusMessage = "Detecting GPUs...";
            int count = await _gpuService.GetDeviceCountAsync();

            GpuList.Clear();

            if (count == 0)
            {
                // Add a dummy entry for systems without NVIDIA GPU
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

    /// <summary>
    /// Refreshes all GPU metrics immediately.
    /// </summary>
    public async Task RefreshMetricsAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing...";

            if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero)
            {
                // No valid GPU selected, set defaults
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

            // Read all metrics in parallel for better performance
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

    /// <summary>
    /// Raises CanExecuteChanged for all control commands.
    /// </summary>
    private void RaiseCanExecuteChanged()
    {
        (SetFanSpeedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SetCoreOffsetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SetMemoryOffsetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SetPowerLimitCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetToDefaultsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ApplyFanCurveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadFanCurveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Sets the fan speed to the target value.
    /// </summary>
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

    /// <summary>
    /// Sets the core clock offset.
    /// </summary>
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

    /// <summary>
    /// Sets the memory clock offset.
    /// </summary>
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

    /// <summary>
    /// Sets the power limit.
    /// </summary>
    private async Task SetPowerLimitAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = $"Setting power limit to {TargetPowerLimit}%...";

            // Get the default power limit to calculate the actual value
            int defaultLimit = await _gpuService.GetPowerLimitAsync(SelectedGpu.Handle);
            if (defaultLimit > 0)
            {
                int targetWatts = (int)(defaultLimit * (TargetPowerLimit / 100.0));
                await _gpuService.SetPowerLimitAsync(SelectedGpu.Handle, targetWatts);
                StatusMessage = $"Power limit set to {targetWatts}W ({TargetPowerLimit}%)";
            }
            else
            {
                // Fallback: try setting as percentage directly
                await _gpuService.SetPowerLimitAsync(SelectedGpu.Handle, TargetPowerLimit);
                StatusMessage = $"Power limit set to {TargetPowerLimit}%";
            }

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

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    private async Task ResetToDefaultsAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Resetting to defaults...";

            // Reset clock offsets
            await _gpuService.SetClockOffsetsAsync(SelectedGpu.Handle, 0, 0);

            // Reset fan speed to auto (0 typically means auto)
            await _gpuService.SetFanSpeedAsync(SelectedGpu.Handle, 0);

            // Reset power limit to default (100%)
            int defaultLimit = await _gpuService.GetPowerLimitAsync(SelectedGpu.Handle);
            if (defaultLimit > 0)
            {
                await _gpuService.SetPowerLimitAsync(SelectedGpu.Handle, defaultLimit);
            }

            // Reset local values
            TargetFanSpeed = 0;
            CoreClockOffset = 0;
            MemoryClockOffset = 0;
            TargetPowerLimit = 100;

            StatusMessage = "Settings reset to defaults";
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

    /// <summary>
    /// Applies the current fan curve to the GPU.
    /// </summary>
    private async Task ApplyFanCurveAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;
        if (FanCurvePoints.Count == 0) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Applying fan curve...";

            var points = new List<NvFanCurvePoint>();
            foreach (var point in FanCurvePoints)
            {
                points.Add(new NvFanCurvePoint
                {
                    Temperature = point.Temperature,
                    SpeedPercent = point.SpeedPercent
                });
            }

            await _gpuService.SetFanCurveAsync(SelectedGpu.Handle, points);

            StatusMessage = $"Fan curve applied ({points.Count} points)";
            await RefreshMetricsAsync();
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

    /// <summary>
    /// Loads the current fan curve from the GPU.
    /// </summary>
    private async Task LoadFanCurveAsync()
    {
        if (SelectedGpu == null || SelectedGpu.Handle == IntPtr.Zero) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Loading fan curve...";

            var curve = await _gpuService.GetFanCurveAsync(SelectedGpu.Handle);

            FanCurvePoints.Clear();
            foreach (var point in curve)
            {
                FanCurvePoints.Add(new NvFanCurvePoint
                {
                    Temperature = point.Temperature,
                    SpeedPercent = point.SpeedPercent
                });
            }

            StatusMessage = $"Fan curve loaded ({curve.Count} points)";
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

    /// <summary>
    /// Resets all offsets to 0 and fan speed to auto.
    /// </summary>
    private async Task ResetAllAsync()
    {
        TargetFanSpeed = 0;
        CoreClockOffset = 0;
        MemoryClockOffset = 0;
        TargetPowerLimit = 100;

        await ResetToDefaultsAsync();
    }

    /// <summary>
    /// Starts real-time monitoring with timer-based polling.
    /// </summary>
    private void StartMonitoring()
    {
        if (IsMonitoring) return;

        _monitoringTimer = new System.Timers.Timer(PollingIntervalMs);
        _monitoringTimer.Elapsed += async (s, e) =>
        {
            await RefreshMetricsAsync();
        };
        _monitoringTimer.AutoReset = true;
        _monitoringTimer.Start();

        IsMonitoring = true;
        StatusMessage = "Monitoring started";
    }

    /// <summary>
    /// Stops real-time monitoring.
    /// </summary>
    private void StopMonitoring()
    {
        if (!IsMonitoring) return;

        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;

        IsMonitoring = false;
        StatusMessage = "Monitoring stopped";
    }

    /// <summary>
    /// Disposes the ViewModel and stops monitoring.
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Simple RelayCommand implementation for ICommand.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
        _execute = () => _ = execute();
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
