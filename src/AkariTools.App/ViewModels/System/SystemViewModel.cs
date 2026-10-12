using AkariTools.App.ViewModels.General;

namespace AkariTools.App.ViewModels.System;

/// <summary>
/// Aggregation host for the System screen's General tab (spec §7.1).
/// The tab ViewModels are constructed by SystemPage code-behind (never in
/// XAML) and handed in here; this type only owns them and fans out loading.
/// Single responsibility (hosting), so no separate GeneralTabViewModel file.
/// </summary>
public sealed class SystemViewModel
{
    public ExplorerViewModel Explorer { get; }

    public ContextMenuViewModel ContextMenu { get; }

    public VisualViewModel Visual { get; }

    public DateTimeViewModel DateTime { get; }

    public SettingsVisibilityViewModel SettingsVisibility { get; }

    public ManualUpdatesViewModel ManualUpdates { get; }

    public PrintServiceViewModel PrintService { get; }

    public GameBarViewModel GameBar { get; }

    public PushNotificationsViewModel PushNotifications { get; }

    public SystemViewModel(
        ExplorerViewModel explorer,
        ContextMenuViewModel contextMenu,
        VisualViewModel visual,
        DateTimeViewModel dateTime,
        SettingsVisibilityViewModel settingsVisibility,
        ManualUpdatesViewModel manualUpdates,
        PrintServiceViewModel printService,
        GameBarViewModel gameBar,
        PushNotificationsViewModel pushNotifications)
    {
        Explorer = explorer;
        ContextMenu = contextMenu;
        Visual = visual;
        DateTime = dateTime;
        SettingsVisibility = settingsVisibility;
        ManualUpdates = manualUpdates;
        PrintService = printService;
        GameBar = gameBar;
        PushNotifications = pushNotifications;
    }

    public async Task LoadAsync()
    {
        await Explorer.LoadAsync();
        await ContextMenu.LoadAsync();
        await Visual.LoadAsync();
        await DateTime.LoadAsync();
        await SettingsVisibility.LoadAsync();
        await ManualUpdates.LoadAsync();
        await PrintService.LoadAsync();
        await GameBar.LoadAsync();
        await PushNotifications.LoadAsync();
    }
}
