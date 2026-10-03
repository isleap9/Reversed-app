using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

/// <summary>
/// View model for the Settings Visibility page.
///
/// Backed by <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\
/// SettingsPageVisibility</c>, the policy the real app writes. The value is a
/// semicolon-separated list prefixed with <c>showonly:</c> or <c>hide:</c>.
/// </summary>
public partial class SettingsVisibilityViewModel : ViewModelBase
{
    private const string PoliciesExplorer = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string VisibilityValueName = "SettingsPageVisibility";

    private readonly IInfoBarService _infoBar;
    private readonly ILogger<SettingsVisibilityViewModel> _logger;

    [ObservableProperty]
    public partial string CurrentValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    /// <summary>Settings pages that can be shown or hidden.</summary>
    public ObservableCollection<SettingsPageEntry> Pages { get; } =
    [
        new("Home", "home"),
        new("System", "system"),
        new("Bluetooth & devices", "bluetooth"),
        new("Network & internet", "network"),
        new("Personalisation", "personalization"),
        new("Apps", "apps"),
        new("Accounts", "accounts"),
        new("Time & language", "timeandlanguage"),
        new("Gaming", "gaming"),
        new("Accessibility", "easeofaccess"),
        new("Privacy & security", "privacy"),
        new("Windows Update", "windowsupdate"),
        new("Update & security", "update"),
    ];

    public SettingsVisibilityViewModel(IInfoBarService infoBar, ILogger<SettingsVisibilityViewModel> logger)
    {
        _infoBar = infoBar;
        _logger = logger;

        Title = "Settings Visibility";
        Load();
    }

    /// <summary>Reads the current policy value.</summary>
    [RelayCommand]
    public void Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PoliciesExplorer, writable: false);
            CurrentValue = key?.GetValue(VisibilityValueName)?.ToString() ?? string.Empty;
            StatusMessage = string.IsNullOrWhiteSpace(CurrentValue)
                ? "No visibility policy set — all Settings pages are visible."
                : $"Policy: {CurrentValue}";

            ApplyPolicyToToggles();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read SettingsPageVisibility");
            StatusMessage = "Could not read the current policy.";
        }
    }

    /// <summary>Writes a <c>hide:</c> policy for the pages currently switched off.</summary>
    [RelayCommand]
    public async Task ApplyAsync()
    {
        var hidden = Pages.Where(p => p.IsHidden).Select(p => p.Id).ToList();

        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.CreateSubKey(PoliciesExplorer, writable: true)
                    ?? throw new InvalidOperationException("Could not open the Policies\\Explorer key.");

                if (hidden.Count == 0)
                {
                    // No pages hidden: remove the policy entirely.
                    key.DeleteValue(VisibilityValueName, throwOnMissingValue: false);
                    return;
                }

                key.SetValue(VisibilityValueName, "hide:" + string.Join(";", hidden), RegistryValueKind.String);
            });

            Load();
            _infoBar.ShowSuccess(
                "Settings visibility applied",
                hidden.Count == 0
                    ? "All Settings pages are visible again."
                    : $"Hiding {hidden.Count} Settings page(s).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write SettingsPageVisibility");
            StatusMessage = $"Could not apply: {ex.Message}";
            _infoBar.ShowError("Could not apply", ex.Message);
        }
    }

    /// <summary>Clears the policy so every Settings page is visible.</summary>
    [RelayCommand]
    public async Task ShowAllAsync()
    {
        foreach (var page in Pages)
        {
            page.IsHidden = false;
        }

        await ApplyAsync();
    }

    private void ApplyPolicyToToggles()
    {
        if (string.IsNullOrWhiteSpace(CurrentValue))
        {
            foreach (var page in Pages)
            {
                page.IsHidden = false;
            }

            return;
        }

        var parts = CurrentValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var mode = parts.FirstOrDefault()?.Split(':')[0] ?? "hide";
        var ids = parts
            .Select(p => p.Contains(':') ? p[(p.IndexOf(':') + 1)..] : p)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var page in Pages)
        {
            var listed = ids.Contains(page.Id);
            // "hide:" lists what is hidden; "showonly:" lists what is visible.
            page.IsHidden = string.Equals(mode, "showonly", StringComparison.OrdinalIgnoreCase)
                ? !listed
                : listed;
        }
    }
}

/// <summary>A Settings page that can be hidden or shown.</summary>
public partial class SettingsPageEntry : ObservableObject
{
    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    public SettingsPageEntry(string displayName, string id)
    {
        DisplayName = displayName;
        Id = id;
    }

    public string DisplayName { get; }

    /// <summary>The identifier used in the SettingsPageVisibility policy.</summary>
    public string Id { get; }
}
