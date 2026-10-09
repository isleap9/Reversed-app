using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Optional Features page. Enables/disables Windows optional
/// features behind an elevation check (T-06-04, D-10); Disable additionally requires
/// an explicit confirmation (D-11) because it can affect system functionality.
/// </summary>
public partial class OptionalFeaturesViewModel : ViewModelBase
{
    private readonly IOptionalFeaturesService _featuresService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<OptionalFeaturesViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasFeatures { get; set; }

    /// <summary>Windows optional features.</summary>
    public ObservableCollection<OptionalFeature> Features { get; } = [];

    public OptionalFeaturesViewModel(
        IOptionalFeaturesService featuresService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<OptionalFeaturesViewModel> logger)
    {
        _featuresService = featuresService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Optional Features";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var features = await _featuresService.GetFeaturesAsync();

            Features.Clear();
            foreach (var feature in features)
            {
                Features.Add(feature);
            }

            HasFeatures = Features.Count > 0;
            StatusMessage = $"{Features.Count} features found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load optional features");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load features: {ex.Message}";
            _infoBar.ShowError("Load failed", "Failed to load optional features. Ensure you have permission to query DISM.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEnableFeature))]
    public async Task EnableFeatureAsync(OptionalFeature? feature)
    {
        if (feature is null)
        {
            return;
        }

        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to enable features. Run Vain Tools as administrator.";
            StatusMessage = "Enable blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Enabling {feature.Name}…";

            await _featuresService.EnableFeatureAsync(feature.Name);

            _infoBar.ShowSuccess("Feature enabled", $"'{feature.Name}' has been enabled.");
            await RefreshAsync();
            StatusMessage = $"Enabled {feature.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable optional feature {Feature}", feature.Name);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not enable feature: {ex.Message}";
            _infoBar.ShowError("Enable failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisableFeature))]
    public async Task DisableFeatureAsync(OptionalFeature? feature)
    {
        if (feature is null)
        {
            return;
        }

        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to disable features. Run Vain Tools as administrator.";
            StatusMessage = "Disable blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Disable Feature",
                $"Are you sure you want to disable '{feature.Name}'? This may affect system functionality.",
                confirmText: "Disable");

            if (!confirmed)
            {
                StatusMessage = "Disable cancelled.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Disabling {feature.Name}…";

            await _featuresService.DisableFeatureAsync(feature.Name);

            _infoBar.ShowSuccess("Feature disabled", $"'{feature.Name}' has been disabled.");
            await RefreshAsync();
            StatusMessage = $"Disabled {feature.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable optional feature {Feature}", feature.Name);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not disable feature: {ex.Message}";
            _infoBar.ShowError("Disable failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanEnableFeature(OptionalFeature? feature) =>
        feature is not null && IsElevated && !feature.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase);

    private bool CanDisableFeature(OptionalFeature? feature) =>
        feature is not null && IsElevated && feature.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase);

    partial void OnIsElevatedChanged(bool value)
    {
        EnableFeatureCommand.NotifyCanExecuteChanged();
        DisableFeatureCommand.NotifyCanExecuteChanged();
    }
}
