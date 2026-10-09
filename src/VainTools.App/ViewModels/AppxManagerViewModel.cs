using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Appx Manager page. Lists installed + provisioned packages
/// and removes them behind an elevation check and a confirmation dialog
/// (threats T-06-01, T-06-07).
/// </summary>
public partial class AppxManagerViewModel : ViewModelBase
{
    private readonly IAppxPackageService _appxService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<AppxManagerViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasPackages { get; set; }

    /// <summary>Installed and provisioned packages.</summary>
    public ObservableCollection<AppxPackage> Packages { get; } = [];

    public AppxManagerViewModel(
        IAppxPackageService appxService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<AppxManagerViewModel> logger)
    {
        _appxService = appxService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Appx Manager";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var packages = await Task.Run(() =>
            {
                var installed = _appxService.GetInstalledPackages();
                var provisioned = _appxService.GetProvisionedPackages();
                return installed.Concat(provisioned).ToList();
            });

            Packages.Clear();
            foreach (var package in packages)
            {
                Packages.Add(package);
            }

            HasPackages = Packages.Count > 0;
            StatusMessage = $"{Packages.Count} packages found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Appx packages");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load packages: {ex.Message}";
            _infoBar.ShowError("Load failed", "Failed to load packages. Ensure you have permission to enumerate Appx packages.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemovePackage))]
    public async Task RemovePackageAsync(AppxPackage? package)
    {
        if (package is null)
        {
            return;
        }

        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to remove packages. Run Vain Tools as administrator.";
            StatusMessage = "Remove blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Remove Package",
                $"Are you sure you want to remove '{package.Name}'? This action cannot be undone.",
                confirmText: "Remove");

            if (!confirmed)
            {
                StatusMessage = "Remove cancelled.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Removing {package.Name}…";

            var result = await _appxService.RemovePackageAsync(package.FullName);
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(result.ErrorText) ? "The package could not be removed." : result.ErrorText);
            }

            _infoBar.ShowSuccess("Package removed", $"'{package.Name}' has been removed.");
            await RefreshAsync();
            StatusMessage = $"Removed {package.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Appx package {FullName}", package.FullName);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not remove package: {ex.Message}";
            _infoBar.ShowError("Remove failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRemovePackage(AppxPackage? package) => package is not null && IsElevated;

    partial void OnIsElevatedChanged(bool value) => RemovePackageCommand.NotifyCanExecuteChanged();
}
