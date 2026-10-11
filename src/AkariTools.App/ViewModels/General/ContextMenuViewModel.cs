using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using System.Collections.ObjectModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Context Menu page: data-driven rows over shell extension registry keys
/// (spec §7.3). Each row is a <see cref="ToggleKind.KeyPresence"/> toggle:
/// the key existing means the feature is present/enabled.
///
/// Rows:
/// - Take Ownership: HKCR\Directory\shell\runas
/// - Send To: HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo
/// - ShellNew .ps1 / .bat / .reg: HKCR\&lt;ext&gt;\ShellNew
/// - Classic context menu: both modern-menu CLSID InprocServer32 keys under
///   HKCU\Software\Classes\CLSID (toggled together via SecondaryValueKey).
/// </summary>
public sealed class ContextMenuViewModel
{
    private static readonly ToggleSetting[] Definitions =
    {
        // Take Ownership — HKCR\Directory\shell\runas (KeyPresence).
        new()
        {
            Id = "TakeOwnership",
            Label = "Take Ownership",
            Description = "Add 'Take Ownership' to the directory context menu.",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", ""),
        },
        // Send To — HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo (KeyPresence).
        new()
        {
            Id = "SendTo",
            Label = "Send To context menu",
            Description = "Enable the Send To handler in the context menu.",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", ""),
        },
        // ShellNew .ps1 — HKCR\.ps1\ShellNew (KeyPresence).
        new()
        {
            Id = "ShellNewPs1",
            Label = "New PowerShell script",
            Description = "Add 'Windows PowerShell Script' to the New menu.",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".ps1\ShellNew", ""),
        },
        // ShellNew .bat — HKCR\.bat\ShellNew (KeyPresence).
        new()
        {
            Id = "ShellNewBat",
            Label = "New batch file",
            Description = "Add 'Batch File' to the New menu.",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".bat\ShellNew", ""),
        },
        // ShellNew .reg — HKCR\.reg\ShellNew (KeyPresence).
        new()
        {
            Id = "ShellNewReg",
            Label = "New registry file",
            Description = "Add " + "\u201C" + "Registration Entry" + "\u201D" + " to the New menu.",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".reg\ShellNew", ""),
        },
        // Classic context menu — both modern-menu CLSID InprocServer32 keys
        // under HKCU\Software\Classes\CLSID (spec §12.5). Toggled together
        // via SecondaryValueKey: both must exist for the row to show On.
        new()
        {
            Id = "ClassicMenu",
            Label = "Classic context menu",
            Description = "Restore the classic right-click menu (disable modern menu).",
            Kind = ToggleKind.KeyPresence,
            ValueKey = new RegistryValue(
                RegistryHive.CurrentUser,
                @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                ""),
            SecondaryValueKey = new RegistryValue(
                RegistryHive.CurrentUser,
                @"Software\Classes\CLSID\{6480400b-5a83-4d1e-9f69-8ae5a8eef5a0}\InprocServer32",
                ""),
        },
    };

    private readonly IRegistrySettingService _registrySettingService;

    public ObservableCollection<SettingRowViewModel> Rows { get; } = new();

    public ContextMenuViewModel(IRegistrySettingService registrySettingService)
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
