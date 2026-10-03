using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Vain Tools page: the application's own settings, including
/// <c>.vain</c> profile import and the restore-defaults flow.
/// </summary>
public partial class VainToolsViewModel : ViewModelBase
{
    /// <summary>Sections the real app lists in its restore-defaults confirmation.</summary>
    private static readonly string[] RestoreDefaultSections = ["Sound", "Security", "Performance"];

    private readonly IVainProfileService _vainProfiles;
    private readonly IFilePickerService _filePicker;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<VainToolsViewModel> _logger;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LastImportSummary { get; set; } = "No profile imported yet.";

    /// <summary>Profiles imported so far, persisted across restarts.</summary>
    public ObservableCollection<VainProfile> ImportedProfiles { get; } = [];

    /// <summary>Human-readable description of what restore-defaults would change.</summary>
    public string RestoreDefaultsDescription =>
        "The following settings will be changed to Vain defaults: " +
        string.Join(", ", RestoreDefaultSections) + ".";

    public VainToolsViewModel(
        IVainProfileService vainProfiles,
        IFilePickerService filePicker,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<VainToolsViewModel> logger)
    {
        _vainProfiles = vainProfiles;
        _filePicker = filePicker;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;

        Title = "Vain Tools";

        _ = LoadImportedAsync();
    }

    private async Task LoadImportedAsync()
    {
        try
        {
            var stored = await _vainProfiles.GetImportedProfilesAsync();
            ImportedProfiles.Clear();
            foreach (var profile in stored)
            {
                ImportedProfiles.Add(profile);
            }

            if (ImportedProfiles.Count > 0)
            {
                LastImportSummary = Describe(ImportedProfiles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load previously imported profiles");
        }
    }

    /// <summary>Opens a file picker and imports the chosen <c>.vain</c> profile.</summary>
    [RelayCommand]
    public async Task ImportProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var file = await _filePicker.PickOpenFileAsync([".vain"]);
            if (file is null)
            {
                StatusMessage = "Import cancelled.";
                return;
            }

            await ImportFromPathAsync(file.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile import failed");
            StatusMessage = "Import failed.";
            _infoBar.ShowError("Import failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Imports a <c>.vain</c> file from a path. Used by both the picker and the
    /// drag-and-drop target.
    /// </summary>
    public async Task ImportFromPathAsync(string path)
    {
        var result = await _vainProfiles.ParseFileAsync(path);

        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            _infoBar.ShowError("Could not import profile", result.ErrorMessage);
            return;
        }

        var document = result.Document!;

        // Replace rather than append: a re-import of the same file should not duplicate.
        ImportedProfiles.Clear();
        foreach (var profile in document.Profiles)
        {
            ImportedProfiles.Add(profile);
        }

        await _vainProfiles.SaveImportedProfilesAsync(ImportedProfiles);

        LastImportSummary = Describe(ImportedProfiles);
        StatusMessage = $"Imported {LastImportSummary}";
        _infoBar.ShowSuccess("Profile imported", $"{result.SourceName}: {LastImportSummary}");
    }

    /// <summary>Restores the documented default set, after confirmation.</summary>
    [RelayCommand]
    public async Task RestoreDefaultsAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Restore Vain Defaults",
            RestoreDefaultsDescription,
            confirmText: "Restore");

        if (!confirmed)
        {
            StatusMessage = "Restore cancelled.";
            return;
        }

        // The individual Sound / Security / Performance settings are implemented in
        // later phases; this records the request and reports accurately rather than
        // claiming a change that did not happen.
        StatusMessage = "Defaults will be applied once the Sound, Security and Performance pages are implemented.";
        _infoBar.ShowInfo(
            "Restore Vain Defaults",
            "No settings were changed. Applying defaults needs the Sound, Security and Performance pages, which arrive in later phases.");
    }

    private static string Describe(IEnumerable<VainProfile> profiles)
    {
        var list = profiles.ToList();
        var settings = list.Sum(p => p.Settings.Count);
        return $"{list.Count} profile(s), {settings} setting(s)";
    }
}
