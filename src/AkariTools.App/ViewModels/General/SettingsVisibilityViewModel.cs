using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Settings Visibility page: toggles for Windows Settings pages
/// (spec §7.6). Each row maps to a Settings URI; hiding a page
/// adds a <c>hide:</c> URI to the <c>SettingsPageVisibility</c>
/// REG_SZ at HKCU\...\Policies\Explorer.
/// </summary>
public sealed partial class SettingsVisibilityViewModel : ObservableObject
{
    private const string PoliciesExplorerPath =
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string SettingsPageVisibilityName = "SettingsPageVisibility";

    private readonly IRegistrySettingService _registrySettingService;

    [ObservableProperty]
    private ObservableCollection<SettingsPageRow> _pages = new();

    [ObservableProperty]
    private bool _isApplying;

    public SettingsVisibilityViewModel(IRegistrySettingService registrySettingService)
    {
        _registrySettingService = registrySettingService;
        BuildRows();
    }

    /// <summary>
    /// The known Settings page list — one per page the original names
    /// in XBF strings. Each entry: Display name + ms-settings: URI.
    /// </summary>
    private static readonly (string Name, string Uri)[] KnownPages =
    {
        ("System", "ms-settings:system"),
        ("Bluetooth & devices", "ms-settings:bluetooth"),
        ("Network & internet", "ms-settings:network"),
        ("Personalization", "ms-settings:personalization"),
        ("Apps", "ms-settings:appsfeatures"),
        ("Accounts", "ms-settings:accounts"),
        ("Time & language", "ms-settings:dateandtime"),
        ("Gaming", "ms-settings:gaming"),
        ("Accessibility", "ms-settings:accessibility"),
        ("Privacy & security", "ms-settings:privacy"),
        ("Windows Update", "ms-settings:windowsupdate"),
        ("Windows Security", "ms-settings:windowsdefender"),
        ("Display", "ms-settings:display"),
        ("Sound", "ms-settings:sound"),
        ("Notifications", "ms-settings:notifications"),
        ("Power & battery", "ms-settings:powersleep"),
        ("Storage", "ms-settings:storage"),
        ("Taskbar", "ms-settings:taskbar"),
        ("Start", "ms-settings:personalization-start"),
        ("File Explorer", "ms-settings:fileexplorer"),
    };

    private void BuildRows()
    {
        Pages.Clear();
        foreach (var (name, uri) in KnownPages)
        {
            Pages.Add(new SettingsPageRow(name, uri, false));
        }
    }

    public async Task LoadAsync()
    {
        var raw = await Task.Run(() => _registrySettingService.ReadString(
            new RegistryValue(
                RegistryHive.CurrentUser,
                PoliciesExplorerPath,
                SettingsPageVisibilityName)));

        var hidden = ParseHiddenSet(raw);

        foreach (var page in Pages)
        {
            page.IsHidden = hidden.Contains(page.Uri);
        }
    }

    /// <summary>
    /// Applies the switch's requested value for one row. The view binds
    /// <see cref="SettingsPageRow.IsHidden"/> OneWay and passes the switch's
    /// new value explicitly, so the VM SETs instead of flipping: a TwoWay
    /// push would already have applied the flip before any handler runs, and
    /// flipping again inverts every user toggle. Same-value calls (revert
    /// echoes after a failed apply) are no-ops. Never throws: failures
    /// revert the row to its previous value.
    /// </summary>
    public async Task SetHiddenAsync(SettingsPageRow row, bool hidden)
    {
        if (row is null || IsApplying || row.IsHidden == hidden)
            return;

        IsApplying = true;
        var previous = row.IsHidden;
        try
        {
            row.IsHidden = hidden;
            var wrote = await Task.Run(() => WriteVisibility());
            if (!wrote)
                row.IsHidden = previous;
        }
        catch
        {
            // Revert on failure.
            row.IsHidden = previous;
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool WriteVisibility()
    {
        var hidden = Pages.Where(p => p.IsHidden).Select(p => p.Uri);
        var value = "hide:" + string.Join(";", hidden);
        return _registrySettingService.WriteString(
            new RegistryValue(
                RegistryHive.CurrentUser,
                PoliciesExplorerPath,
                SettingsPageVisibilityName),
            value);
    }

    private static HashSet<string> ParseHiddenSet(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return set;

        // Format: "hide:uri1;uri2;uri3" (single REG_SZ value).
        if (raw.StartsWith("hide:", StringComparison.OrdinalIgnoreCase))
            raw = raw[5..];

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                set.Add(trimmed);
        }
        return set;
    }
}

/// <summary>
/// One Settings page row: display name, ms-settings: URI, hidden flag.
/// </summary>
public sealed partial class SettingsPageRow : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _uri;

    [ObservableProperty]
    private bool _isHidden;

    public SettingsPageRow(string name, string uri, bool isHidden)
    {
        _name = name;
        _uri = uri;
        _isHidden = isHidden;
    }
}
