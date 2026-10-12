using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Visual page: data-driven rows over personalization and effects
/// keys (spec §7.4). <c>AppsUseLightTheme</c> is read as display-only
/// text — the app theme system owns theme switching, so it is rendered
/// as a read-only row, never a toggle.
/// </summary>
public sealed partial class VisualViewModel : ObservableObject
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    private static readonly ToggleSetting[] Definitions =
    {
        // Source: HKCU\...\Themes\Personalize\EnableTransparency (DWORD, 1 = on).
        new()
        {
            Id = "EnableTransparency",
            Label = "Transparency effects",
            Description = "Enable acrylic and transparency effects.",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, PersonalizePath, "EnableTransparency"),
            OnValue = 1,
            OffValue = 0,
        },
        // Source: HKCU\...\Explorer\Advanced\TaskbarAnimations (DWORD, 1 = on).
        new()
        {
            Id = "TaskbarAnimations",
            Label = "Taskbar animations",
            Description = "Animate taskbar buttons.",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "TaskbarAnimations"),
            OnValue = 1,
            OffValue = 0,
        },
        // Source: HKCU\...\Explorer\Advanced\ListviewAlphaSelect (DWORD, 1 = on).
        new()
        {
            Id = "ListviewAlphaSelect",
            Label = "Translucent selection box in File Explorer",
            Description = "Show a translucent selection box around selected items.",
            Kind = ToggleKind.Dword,
            ValueKey = new RegistryValue(RegistryHive.CurrentUser, AdvancedPath, "ListviewAlphaSelect"),
            OnValue = 1,
            OffValue = 0,
        },
    };

    private readonly IRegistrySettingService _registrySettingService;

    public ObservableCollection<SettingRowViewModel> Rows { get; } = new();

    [ObservableProperty]
    private string _appsUseLightThemeText = "Off";

    public VisualViewModel(IRegistrySettingService registrySettingService)
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

        // AppsUseLightTheme (DWORD, 1 = light): display-only (spec §7.4).
        var appsUseLightTheme = await Task.Run(() => _registrySettingService.ReadDword(
            new RegistryValue(RegistryHive.CurrentUser, PersonalizePath, "AppsUseLightTheme")));
        AppsUseLightThemeText = appsUseLightTheme == 1 ? "On" : "Off";
    }
}
