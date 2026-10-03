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

    /// <summary>
    /// Initializes a new instance of the GpuGovernorViewModel.
    /// </summary>
    /// <param name="gpuService">The GPU service for reading metrics.</param>
    public GpuGovernorViewModel(IGpuService gpuService)
    {
        _gpuService = gpuService ?? throw new ArgumentNullException(nameof(gpuService));

        GpuList = new ObservableCollection<GpuInfo>();

        RefreshCommand = new RelayCommand(async () => await RefreshMetricsAsync(), () => !IsRefreshing);
        StartMonitoringCommand = new RelayCommand(StartMonitoring, () => !IsMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);

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
