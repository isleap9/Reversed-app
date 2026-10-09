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
/// that shows the raw command, the resolved program, the arguments and the registry key
/// the entry came from (T-06-02, T-06-15, D-11).
/// </summary>
public partial class InstalledAppsViewModel : ViewModelBase
{
    private readonly IInstalledAppsService _appsService;
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

    /// <summary>
    /// Zero-one-many summary for the card header ("No programs" / "1 program" / "N programs").
    /// </summary>
    [ObservableProperty]
    public partial string ProgramCountText { get; set; } = "No programs";

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

            // Offloaded so the UI thread is never blocked, but the await below does NOT use
            // ConfigureAwait(false): the continuation touches bound collections and must
            // resume on the UI thread.
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
            _infoBar.ShowError(
                "Load failed",
                "Failed to load installed programs. Ensure you have permission to read the registry.");
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

        // D-10: elevation first — never offer a destructive action that cannot succeed.
        if (!IsElevated)
        {
            ErrorMessage = "Administrator rights required to uninstall programs. Run Vain Tools as administrator.";
            StatusMessage = "Uninstall blocked: elevation required.";
            _infoBar.ShowError("Elevation required", ErrorMessage);
            return;
        }

        // D-04: prefer the quiet (unattended) command when the publisher registered one.
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
            // D-11 / T-06-02 / T-06-15: explicit confirmation, and the raw command is shown so
            // the user can see exactly what will be executed. Only the executable token is
            // split off here — the argument text reaches the uninstaller byte-for-byte, so no
            // shell metacharacter or %VAR% is ever expanded (see UninstallCommandLine).
            var launch = UninstallCommandLine.Parse(command);

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

            var exitCode = await RunUninstallAsync(launch);
            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"The uninstaller for '{app.DisplayName}' exited with code {exitCode}.");
            }

            // D-13: reload so the list reflects reality after a destructive operation.
            await RefreshAsync();
            _infoBar.ShowSuccess("Uninstaller finished", $"The uninstaller for '{app.DisplayName}' has exited.");
            StatusMessage = $"Uninstalled {app.DisplayName}";
        }
        catch (Exception ex)
        {
            // D-12 / T-06-07: full detail in the log, a user-friendly line on screen.
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
            // D-04 / T-06-08: only the explicit user-requested raw string is copied.
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
    ///
    /// <para>
    /// The command line is handed over the way Windows itself hands one to a new process:
    /// <see cref="ProcessStartInfo.FileName"/> holds only the resolved executable and
    /// <see cref="ProcessStartInfo.Arguments"/> holds the remaining registry text verbatim.
    /// Shell execution stays off (<see cref="ProcessStartInfo.UseShellExecute"/> is false) and
    /// no command interpreter is involved, so nothing inside the registry string — not
    /// <c>&amp;</c>, <c>|</c>, <c>^</c> or <c>%VAR%</c> — is expanded before the uninstaller,
    /// which inherits administrator rights, reads it (T-06-15).
    /// </para>
    ///
    /// <para>Overridable for tests so unit tests never launch real uninstallers.</para>
    /// </summary>
    protected virtual async Task<int> RunUninstallAsync(UninstallLaunch launch)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            // The raw string property, NOT ArgumentList: .NET quotes each ArgumentList element,
            // and MsiExec / NSIS parse their own raw command line, so re-quoting would change
            // what the uninstaller receives (CR-01).
            Arguments = launch.Arguments,
            UseShellExecute = false,
            // A GUI uninstaller shows its own window; a console one gets a console.
            CreateNoWindow = false,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The uninstaller could not be started.");

        await process.WaitForExitAsync();

        _logger.LogInformation(
            "Uninstaller {FileName} {Arguments} exited with {ExitCode}",
            launch.FileName,
            launch.Arguments,
            process.ExitCode);

        return process.ExitCode;
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
        ProgramCountText = CountPrograms(Programs.Count);
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

    private static string CountPrograms(int count) => count switch
    {
        0 => "No programs",
        1 => "1 program",
        _ => $"{count} programs",
    };

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnIsElevatedChanged(bool value) => UninstallCommand.NotifyCanExecuteChanged();
}
