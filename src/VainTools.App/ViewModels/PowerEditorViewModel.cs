using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public sealed partial class PowerEditorViewModel : ViewModelBase
{
    private readonly IPowerService _powerService;
    private readonly ILogger<PowerEditorViewModel> _logger;

    public PowerEditorViewModel(IPowerService powerService, ILogger<PowerEditorViewModel> logger)
    {
        _powerService = powerService;
        _logger = logger;

        LoadPlansCommand = new AsyncRelayCommand(LoadPlansAsync);
        LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        RevertChangesCommand = new AsyncRelayCommand(RevertChangesAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<PowerPlan> Plans { get; } = [];
    public ObservableCollection<PowerSetting> Settings { get; } = [];

    [ObservableProperty] public partial PowerPlan? SelectedPlan { get; set; }
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; } = "Ready";

    public bool IsElevated => _powerService.IsElevated;

    public bool CanApply => IsElevated && !IsBusy && SelectedPlan is not null;
    public bool CanRevert => IsElevated && !IsBusy && SelectedPlan is not null;

    public IAsyncRelayCommand LoadPlansCommand { get; }
    public IAsyncRelayCommand LoadSettingsCommand { get; }
    public IAsyncRelayCommand ApplyChangesCommand { get; }
    public IAsyncRelayCommand RevertChangesCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task LoadPlansAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading power plans...";
        try
        {
            var plans = await _powerService.GetPlansAsync();
            Plans.Clear();
            foreach (var plan in plans)
                Plans.Add(plan);

            SelectedPlan = Plans.FirstOrDefault(p => p.IsActive) ?? Plans.FirstOrDefault();
            StatusMessage = $"Loaded {plans.Count} power plans.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load power plans");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadSettingsAsync()
    {
        if (SelectedPlan is null) return;
        IsBusy = true;
        StatusMessage = "Loading settings...";
        try
        {
            var settings = await _powerService.GetSettingsAsync(SelectedPlan.Guid);
            Settings.Clear();
            foreach (var setting in settings)
                Settings.Add(setting);
            StatusMessage = $"Loaded {settings.Count} settings for {SelectedPlan.Name}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ApplyChangesAsync()
    {
        if (SelectedPlan is null) return;
        IsBusy = true;
        StatusMessage = "Applying changes...";
        try
        {
            foreach (var setting in Settings)
            {
                if (!string.IsNullOrEmpty(setting.CurrentAcValue))
                {
                    await _powerService.ApplySettingAsync(
                        SelectedPlan.Guid, setting.Guid, setting.CurrentAcValue);
                }
            }
            StatusMessage = "Changes applied successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply changes");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RevertChangesAsync()
    {
        if (SelectedPlan is null) return;
        IsBusy = true;
        StatusMessage = "Reverting to defaults...";
        try
        {
            await _powerService.RevertPlanAsync(SelectedPlan.Guid);
            StatusMessage = "Power plans restored to defaults.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert changes");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshAsync()
    {
        await LoadPlansAsync();
        await LoadSettingsAsync();
    }
}
