using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Installed Apps page. Lists uninstall-registry programs with
/// uninstall (direct <c>Process.Start</c>, D-05) and copy-command (raw string, D-04).
///
/// Uninstall is gated on elevation (T-06-03, D-10) and an explicit confirmation dialog
/// that shows the raw command (T-06-02, D-11).
/// </summary>
public partial class InstalledAppsViewModel : ViewModelBase
{
    private readonly IInstalledAppsService _appsService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<InstalledAppsViewModel> _logger;
    private readonly List<InstalledApp> _allPrograms = [];

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
    public partial bool HasPrograms { get; set; }

    /// <summary>Programs matching the current <see cref="SearchQuery"/>.</summary>
    public ObservableCollection<InstalledApp> Programs { get; } = [];

    public InstalledAppsViewModel(
        IInstalledAppsService appsService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<InstalledAppsViewModel> logger)
    {
        _appsService = appsService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Installed Apps";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var programs = await Task.Run(() => _appsService.GetInstalledApps());

            _allPrograms.Clear();
            _allPrograms.AddRange(programs);
            ApplyFilter();

            StatusMessage = $"{_allPrograms.Count} programs found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load installed programs");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not load programs: {ex.Message}";
            _infoBar.ShowError("Load failed", "Failed to load installed programs. Ensure you have permission to read the registry.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    public async Task UninstallAsync(InstalledApp? app)
    {
        if (app is null)
        {
            return;
        }

        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to uninstall programs. Run Vain Tools as administrator.";
            StatusMessage = "Uninstall blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        var command = SelectUninstallCommand(app);
        if (string.IsNullOrWhiteSpace(command))
        {
            ErrorMessage = $"No uninstall command is registered for '{app.DisplayName}'.";
            StatusMessage = $"Could not uninstall: no command for {app.DisplayName}.";
            _infoBar.ShowError("Uninstall failed", ErrorMessage);
            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Uninstall",
                $"Are you sure you want to uninstall '{app.DisplayName}'? This will run the program's uninstaller.{Environment.NewLine}{Environment.NewLine}Command:{Environment.NewLine}{command}",
                confirmText: "Uninstall");

            if (!confirmed)
            {
                StatusMessage = "Uninstall cancelled.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Uninstalling {app.DisplayName}…";

            await RunUninstallAsync(command);

            _infoBar.ShowSuccess("Uninstall started", $"The uninstaller for '{app.DisplayName}' has finished.");
            await RefreshAsync();
            StatusMessage = $"Uninstalled {app.DisplayName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall {Name}", app.DisplayName);
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not uninstall: {ex.Message}";
            _infoBar.ShowError("Uninstall failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyCommand))]
    public void CopyCommand(InstalledApp? app)
    {
        if (app is null)
        {
            return;
        }

        try
        {
            CopyToClipboardCore(app.UninstallString);
            StatusMessage = "Uninstall command copied to clipboard.";
            _infoBar.ShowSuccess("Copied", "Uninstall command copied to clipboard.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy uninstall command");
            ErrorMessage = ex.Message;
            StatusMessage = $"Could not copy command: {ex.Message}";
            _infoBar.ShowError("Copy failed", ex.Message);
        }
    }

    /// <summary>
    /// Prefers the quiet (unattended) uninstall command when the publisher registered one (D-04).
    /// </summary>
    public static string SelectUninstallCommand(InstalledApp app) =>
        !string.IsNullOrWhiteSpace(app.QuietUninstallString)
            ? app.QuietUninstallString
            : app.UninstallString;

    /// <summary>
    /// Launches the uninstaller directly via <c>Process.Start</c> (D-05) — deliberately NOT
    /// through <c>IProcessRunner</c>, whose redirected-stream setup is for capturing CLI output.
    /// Overridable for tests so unit tests never launch real uninstallers.
    /// </summary>
    protected virtual async Task RunUninstallAsync(string command)
    {
        var startInfo = new ProcessStartInfo(command) { UseShellExecute = true };
        using var process = Process.Start(startInfo);
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }

    /// <summary>
    /// Copies text to the clipboard. Overridable for tests (no clipboard in the test host).
    /// </summary>
    protected virtual void CopyToClipboardCore(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    private bool CanUninstall(InstalledApp? app) => app is not null && IsElevated;

    private static bool CanCopyCommand(InstalledApp? app) =>
        app is not null && !string.IsNullOrWhiteSpace(app.UninstallString);

    private void ApplyFilter()
    {
        Programs.Clear();
        foreach (var program in _allPrograms.Where(MatchesFilter))
        {
            Programs.Add(program);
        }

        HasPrograms = Programs.Count > 0;
    }

    private bool MatchesFilter(InstalledApp app)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return true;
        }

        return app.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            || app.Publisher.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            || app.DisplayVersion.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnIsElevatedChanged(bool value) => UninstallCommand.NotifyCanExecuteChanged();
}
