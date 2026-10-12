using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using System.Collections.ObjectModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Explorer page: data-driven rows over well-known Explorer values
/// (spec §7.2). Every definition is a DWORD under
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced,
/// 1 = on. Explorer is never restarted automatically — the page
/// carries the "restart Explorer" note instead.
/// </summary>
public sealed class ExplorerViewModel
{
    private const string AdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    private static readonly ToggleSetting[] Definitions =
    {
        // Source: HKCU\...\Explorer\Advanced\HideFileExt (DWORD, 1 = hide).
        new()
        {
            Id = "HideFileExt",
            Label = "Hide file extensions",
            Description = "Hide file name extensions for known file types.",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "HideFileExt"),
            OnValue = 1,
            OffValue = 0,
        },
        // Source: HKCU\...\Explorer\Advanced\Hidden (DWORD, 1 = show).
        new()
        {
            Id = "Hidden",
            Label = "Show hidden files",
            Description = "Show files and folders marked hidden.",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "Hidden"),
            OnValue = 1,
            OffValue = 0,
        },
        // Source: HKCU\...\Explorer\Advanced\ShowSuperHidden (DWORD, 1 = show).
        new()
        {
            Id = "ShowSuperHidden",
            Label = "Show protected operating system files",
            Description = "Show protected operating system files (recommended off).",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "ShowSuperHidden"),
            OnValue = 1,
            OffValue = 0,
        },
        // Source: HKCU\...\Explorer\Advanced\TaskbarDa (DWORD, 1 = on, Windows 11).
        // This is the Widgets board toggle — labeled honestly (was: "Show
        // taskbar on all displays", which names a different setting).
        new()
        {
            Id = "TaskbarDa",
            Label = "Widgets board",
            Description = "Show or hide the Widgets board (TaskbarDa, Windows 11).",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "TaskbarDa"),
            OnValue = 1,
            OffValue = 0,
        },
    };

    private readonly IRegistrySettingService _registrySettingService;

    public ObservableCollection<SettingRowViewModel> Rows { get; } = new();

    public ExplorerViewModel(IRegistrySettingService registrySettingService)
    {
        _registrySettingService = registrySettingService;
    }

    public async Task LoadAsync()
    {
        Rows.Clear();
        foreach (var definition in Definitions)
        {
            var row = new SettingRowViewModel(definition, _registrySettingService);
            await row.LoadAsync();
            Rows.Add(row);
        }
    }
}
