using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using System.Collections.ObjectModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Context Menu page: seven data-driven <see cref="ToggleKind.KeyValues"/>
/// rows over shell extension registry keys. Every row probes one string
/// value for state; enabling writes the row's value set (REG_SZ or
/// REG_EXPAND_SZ) and disabling deletes the row's key trees.
/// All key paths/values from binary evidence 2026-10-11
/// (spec §13 — SUPERSEDES the two-CLSID sketch in §7.3/§12.5).
/// </summary>
public sealed class ContextMenuViewModel
{
    // takeown + icacls command backing the Take Ownership entry
    // (binary evidence 2026-10-11).
    private const string TakeOwnershipCommand =
        "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant *S-1-5-32-544:F /t";

    private const string ViewsKey1 = @"Software\Classes\CLSID\{2aa9162e-c906-4dd9-ad0b-3d24a8eef5a0}";
    private const string ViewsKey2 = @"Software\Classes\CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}";

    private static ToggleSetting ModernViewsRow()
    {
        List<RegistryStringWrite> WritesFor(string clsid) => new()
        {
            new(new RegistryValue(RegistryHive.CurrentUser, clsid, ""), ""),
            new(new RegistryValue(RegistryHive.CurrentUser, clsid + @"\InprocServer32", ""), ""),
            new(new RegistryValue(RegistryHive.CurrentUser, clsid + @"\InprocServer32", "ThreadingModel"), "Apartment"),
        };

        var writes = WritesFor(ViewsKey1);
        writes.AddRange(WritesFor(ViewsKey2));

        return new ToggleSetting
        {
            Id = "ModernViews",
            Label = "Modern File Explorer Views",
            Description = "Toggle the modern File Explorer view handlers.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, ViewsKey1, ""),
            Probe = new RegistryValue(RegistryHive.CurrentUser, ViewsKey1, ""),
            Writes = writes,
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.CurrentUser, ViewsKey1, ""),
                new(RegistryHive.CurrentUser, ViewsKey2, ""),
            },
        };
    }

    private static readonly ToggleSetting[] Definitions =
    {
        // Modern Context Menu — HKCU modern-menu CLSID (binary evidence 2026-10-11).
        new()
        {
            Id = "ModernMenu",
            Label = "Modern Context Menu",
            Description = "Toggle the modern Windows 11 context menu handler.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(
                RegistryHive.CurrentUser,
                @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                ""),
            Probe = new RegistryValue(
                RegistryHive.CurrentUser,
                @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                ""),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(
                    RegistryHive.CurrentUser,
                    @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                    ""), ""),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.CurrentUser,
                    @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}",
                    ""),
            },
        },
        // Take Ownership — HKCR\Directory\shell\runas + command (binary evidence 2026-10-11).
        new()
        {
            Id = "TakeOwnership",
            Label = "Take Ownership",
            Description = "Add 'Take Ownership' to the directory context menu.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", ""),
            Probe = new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", ""),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", ""), "Take Ownership"),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", "HasLUAShield"), ""),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas", "NoWorkingDirectory"), ""),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas\command", ""), TakeOwnershipCommand),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas\command", "IsolatedCommand"), TakeOwnershipCommand),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.ClassesRoot, @"Directory\shell\runas", ""),
            },
        },
        // Send To — two handler registrations (binary evidence 2026-10-11).
        new()
        {
            Id = "SendTo",
            Label = "Send To",
            Description = "Enable the Send To handler in the context menu.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(
                RegistryHive.ClassesRoot,
                @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
                ""),
            Probe = new RegistryValue(
                RegistryHive.ClassesRoot,
                @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
                ""),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(
                    RegistryHive.ClassesRoot,
                    @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
                    ""), "{7BA4C740-9E81-11CF-99D3-00AA004AE837}"),
                new(new RegistryValue(
                    RegistryHive.ClassesRoot,
                    @"UserLibraryFolder\shellex\ContextMenuHandlers\SendTo",
                    ""), "{7BA4C740-9E81-11CF-99D3-00AA004AE837}"),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.ClassesRoot, @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", ""),
                new(RegistryHive.ClassesRoot, @"UserLibraryFolder\shellex\ContextMenuHandlers\SendTo", ""),
            },
        },
        // New Windows PowerShell Script — .ps1 ShellNew set (binary evidence 2026-10-11).
        new()
        {
            Id = "NewPs1",
            Label = "New Windows PowerShell Script",
            Description = "Add 'Windows PowerShell Script' to the New menu.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".ps1\ShellNew", "NullFile"),
            Probe = new RegistryValue(RegistryHive.ClassesRoot, @".ps1\ShellNew", "NullFile"),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(RegistryHive.ClassesRoot, @".ps1", ""), "Microsoft.PowerShellScript.1"),
                new(new RegistryValue(RegistryHive.ClassesRoot, @".ps1\ShellNew", "NullFile"), ""),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Microsoft.PowerShellScript.1", ""), "Windows PowerShell Script"),
                new(new RegistryValue(RegistryHive.ClassesRoot, @"Microsoft.PowerShellScript.1", "FriendlyTypeName"), "Windows PowerShell Script"),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.ClassesRoot, @".ps1\ShellNew", ""),
            },
        },
        // New Batch File — NullFile (REG_SZ) + ItemName (REG_EXPAND_SZ) (binary evidence 2026-10-11).
        new()
        {
            Id = "NewBat",
            Label = "New Batch File",
            Description = "Add 'Batch File' to the New menu.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".bat\ShellNew", "NullFile"),
            Probe = new RegistryValue(RegistryHive.ClassesRoot, @".bat\ShellNew", "NullFile"),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(RegistryHive.ClassesRoot, @".bat\ShellNew", "NullFile"), ""),
                new(new RegistryValue(RegistryHive.ClassesRoot, @".bat\ShellNew", "ItemName"),
                    @"%systemroot%\System32\acppage.dll,-6002", ExpandString: true),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.ClassesRoot, @".bat\ShellNew", ""),
            },
        },
        // New Registry File — NullFile (REG_SZ) + ItemName (REG_EXPAND_SZ) (binary evidence 2026-10-11).
        new()
        {
            Id = "NewReg",
            Label = "New Registry File",
            Description = "Add 'Registration Entry' to the New menu.",
            Kind = ToggleKind.KeyValues,
            ValueKey = new RegistryValue(RegistryHive.ClassesRoot, @".reg\ShellNew", "NullFile"),
            Probe = new RegistryValue(RegistryHive.ClassesRoot, @".reg\ShellNew", "NullFile"),
            Writes = new List<RegistryStringWrite>
            {
                new(new RegistryValue(RegistryHive.ClassesRoot, @".reg\ShellNew", "NullFile"), ""),
                new(new RegistryValue(RegistryHive.ClassesRoot, @".reg\ShellNew", "ItemName"),
                    @"%systemroot%\regedit.exe,-309", ExpandString: true),
            },
            Deletes = new List<RegistryValue>
            {
                new(RegistryHive.ClassesRoot, @".reg\ShellNew", ""),
            },
        },
        ModernViewsRow(),
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
