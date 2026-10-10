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
/// (threats T-06-01, T-06-07; decisions D-10, D-11, D-13).
/// </summary>
public partial class AppxManagerViewModel : ViewModelBase
{
    private readonly IAppxPackageService _appxService;
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

    /// <summary>
    /// Zero-one-many summary for the card header ("No packages" / "1 package" / "N packages").
    /// </summary>
    [ObservableProperty]
    public partial string PackageCountText { get; set; } = "No packages";

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

            // WinRT enumeration is offloaded so the UI thread is never blocked, but the
            // await below deliberately does NOT use ConfigureAwait(false): the
            // continuation touches bound collections and must resume on the UI thread.
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
            PackageCountText = CountPackages(Packages.Count);
            StatusMessage = $"{Packages.Count} packages found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Appx packages");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load packages: {ex.Message}";
            _infoBar.ShowError(
                "Load failed",
                "Failed to load packages. Ensure you have permission to enumerate Appx packages.");
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

        // D-10: elevation first — never offer a destructive action that cannot succeed.
        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to remove packages. Run Vain Tools as administrator.";
            StatusMessage = "Remove blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        // A provisioned row without a family name cannot be deprovisioned (T-06-24).
        if (package.IsProvisioned && string.IsNullOrWhiteSpace(package.PackageFamilyName))
        {
            ErrorMessage = "This provisioned package has no package family name, so it cannot be deprovisioned.";
            StatusMessage = "Remove blocked: no package family name.";
            _infoBar.ShowError("Remove failed", ErrorMessage);
            return;
        }

        try
        {
            // D-11: explicit confirmation naming the scope for provisioned rows.
            var confirmed = await _dialogs.ConfirmAsync(
                package.IsProvisioned ? "Remove Provisioned Package" : "Remove Package",
                BuildRemoveConfirmation(package),
                confirmText: "Remove");

            if (!confirmed)
            {
                StatusMessage = "Remove cancelled.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Removing {package.Name}…";

            DeploymentResult result;
            if (package.IsProvisioned)
            {
                result = await _appxService.DeprovisionPackageAsync(package.PackageFamilyName);
            }
            else
            {
                result = await _appxService.RemovePackageAsync(package.FullName);
            }

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(result.ErrorText)
                        ? "The package could not be removed."
                        : result.ErrorText);
            }

            if (package.IsProvisioned)
            {
                _infoBar.ShowSuccess("Package deprovisioned", $"'{package.Name}' is no longer provisioned for new users.");
            }
            else
            {
                _infoBar.ShowSuccess("Package removed", $"'{package.Name}' has been removed.");
            }

            // D-13: reload so the list reflects reality after a destructive operation.
            await RefreshAsync();
            StatusMessage = package.IsProvisioned ? $"Deprovisioned {package.Name}" : $"Removed {package.Name}";
        }
        catch (Exception ex)
        {
            // D-12 / T-06-07: full detail in the log, a user-friendly line on screen.
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

    /// <summary>
    /// Builds the confirmation message for a package removal. Provisioned rows state
    /// the all-users scope and name the package family; installed rows keep the
    /// existing per-user copy.
    /// </summary>
    public static string BuildRemoveConfirmation(AppxPackage package)
    {
        if (package.IsProvisioned)
        {
            return $"Remove the provisioned package '{package.Name}'?\n\n"
                + "It will be deprovisioned for all users: new user accounts will no longer get this app. "
                + "Copies already installed for existing users are not removed.\n\n"
                + $"Package family: {package.PackageFamilyName}\n\n"
                + "This cannot be undone from Vain Tools.";
        }

        return $"Are you sure you want to remove '{package.Name}'? This action cannot be undone.";
    }

    private bool CanRemovePackage(AppxPackage? package) => package is not null && IsElevated && !IsLoading;

    private static string CountPackages(int count) => count switch
    {
        0 => "No packages",
        1 => "1 package",
        _ => $"{count} packages",
    };

    partial void OnIsElevatedChanged(bool value) => RemovePackageCommand.NotifyCanExecuteChanged();

    partial void OnIsLoadingChanged(bool value) => RemovePackageCommand.NotifyCanExecuteChanged();
}
