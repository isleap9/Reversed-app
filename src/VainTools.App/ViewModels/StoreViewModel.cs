using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Store page. Searches installable apps through the winget CLI
/// (D-08) and installs one behind an elevation check (T-06-12, D-10) and an explicit
/// confirmation that names the app and its package id (T-06-10, D-11) — a winget
/// install is a real system mutation.
/// </summary>
public partial class StoreViewModel : ViewModelBase
{
    private readonly IStoreService _storeService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<StoreViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasApps { get; set; }

    /// <summary>
    /// Zero-one-many summary for the card header ("No apps" / "1 app" / "N apps").
    /// </summary>
    [ObservableProperty]
    public partial string AppCountText { get; set; } = "No apps";

    /// <summary>Apps reported by the most recent search.</summary>
    public ObservableCollection<StoreApp> Apps { get; } = [];

    public StoreViewModel(
        IStoreService storeService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<StoreViewModel> logger)
    {
        _storeService = storeService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Store";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        var query = SearchQuery;
        if (string.IsNullOrWhiteSpace(query))
        {
            ErrorMessage = "Enter a search term to list installable apps.";
            StatusMessage = "Search skipped: no query.";
            _infoBar.ShowError("Search skipped", ErrorMessage);
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // The service blocks on the winget run (its contract is synchronous), so the
            // work is offloaded. The await deliberately omits ConfigureAwait(false): the
            // continuation clears and repopulates a bound ObservableCollection and must
            // resume on the UI thread, or WinUI throws RPC_E_WRONG_THREAD.
            var apps = await Task.Run(() => _storeService.SearchApps(query));

            Apps.Clear();
            foreach (var app in apps)
            {
                Apps.Add(app);
            }

            HasApps = Apps.Count > 0;
            AppCountText = CountApps(Apps.Count);
            StatusMessage = $"{Apps.Count} apps found";
        }
        catch (Exception ex)
        {
            // D-12 / T-06-14: full detail in the log, one user-friendly sentence on screen.
            _logger.LogError(ex, "Failed to search apps for \"{Query}\"", query);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not search apps: {ex.Message}";
            _infoBar.ShowError(
                "Search failed",
                "Failed to load apps. Ensure winget is available and you have a network connection.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallApp))]
    public async Task InstallAppAsync(StoreApp? app)
    {
        if (app is null)
        {
            return;
        }

        // D-10 / T-06-12: elevation first — winget installs machine-wide, so never
        // offer an action that cannot succeed.
        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to install apps. Run Vain Tools as administrator.";
            StatusMessage = $"{app.Name}: install blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        try
        {
            // D-11 / T-06-10: the confirmation names the app AND its package id and shows
            // the command, so a display name alone cannot disguise what will be installed.
            var confirmed = await _dialogs.ConfirmAsync(
                "Install",
                $"Are you sure you want to install '{app.Name}'?{Environment.NewLine}{Environment.NewLine}winget will run: install --id {app.Id} --exact{Environment.NewLine}This installs the app for all users of this PC.",
                confirmText: "Install");

            if (!confirmed)
            {
                StatusMessage = $"Install of {app.Name} cancelled.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Installing {app.Name}…";

            var result = await _storeService.InstallAppAsync(app.Id);

            if (result.ExitCode == 0)
            {
                _infoBar.ShowSuccess("App installed", $"'{app.Name}' has been installed.");
                StatusMessage = $"Installed {app.Name}";
                return;
            }

            // A winget failure is a normal outcome, not an exception: report it with the
            // CLI's own message, which is written for a console user.
            var detail = result.StdErr.Trim();
            _logger.LogWarning(
                "winget install of {AppId} failed (exit {ExitCode}). {StdErr}",
                app.Id, result.ExitCode, detail);
            ErrorMessage = detail.Length > 0
                ? $"winget could not install '{app.Name}'. {detail}"
                : $"winget could not install '{app.Name}' (exit code {result.ExitCode}).";
            StatusMessage = $"Could not install {app.Name}.";
            _infoBar.ShowError("Install failed", ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install {AppId}", app.Id);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not install {app.Name}: {ex.Message}";
            _infoBar.ShowError(
                "Install failed",
                "Failed to install the app. Ensure winget is available and you have a network connection.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        // The Store list is a search result, not machine state, so a refresh re-runs the
        // current query rather than reloading a snapshot.
        await SearchAsync();
    }

    private bool CanInstallApp(StoreApp? app)
        => app is not null && IsElevated && !IsLoading;

    private static string CountApps(int count) => count switch
    {
        0 => "No apps",
        1 => "1 app",
        _ => $"{count} apps",
    };

    partial void OnIsElevatedChanged(bool value)
        => InstallAppCommand.NotifyCanExecuteChanged();

    partial void OnIsLoadingChanged(bool value)
        => InstallAppCommand.NotifyCanExecuteChanged();
}
