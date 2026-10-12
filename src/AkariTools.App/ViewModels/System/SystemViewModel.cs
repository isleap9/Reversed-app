using AkariTools.App.ViewModels.General;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using AkariTools.Framework.Services;

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

    private readonly ICliRunner _cliRunner;
    private readonly XamlRoot _xamlRoot;

    public SystemViewModel(
        ExplorerViewModel explorer,
        ContextMenuViewModel contextMenu,
        VisualViewModel visual,
        DateTimeViewModel dateTime,
        SettingsVisibilityViewModel settingsVisibility,
        ManualUpdatesViewModel manualUpdates,
        PrintServiceViewModel printService,
        GameBarViewModel gameBar,
        PushNotificationsViewModel pushNotifications,
        ICliRunner cliRunner,
        XamlRoot xamlRoot)
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

        _cliRunner = cliRunner;
        _xamlRoot = xamlRoot;

        RestartExplorerCommand = new RelayCommand(async () => await RestartExplorerAsync());
        ApplyDefaultsCommand = new RelayCommand(async () => await ApplyDefaultsAsync());
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
    }

    /// <summary>
    /// Restarts Explorer: taskkill /f /im explorer.exe then relaunches via shell open.
    /// </summary>
    public RelayCommand RestartExplorerCommand { get; }

    /// <summary>
    /// Applies Vain defaults: confirms a ContentDialog and resets the four segmented
    /// VMs to index 1 (Enabled) with backend writes; DateTime combos are left unchanged.
    /// </summary>
    public RelayCommand ApplyDefaultsCommand { get; }

    /// <summary>
    /// Refreshes all tab state by re-reading each VM via LoadAsync.
    /// </summary>
    public RelayCommand RefreshCommand { get; }

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

    private async Task RestartExplorerAsync()
    {
        // taskkill /f /im explorer.exe then ShellExecuteW("explorer.exe", "open")
        _cliRunner.Run("taskkill", "/f /im explorer.exe");
        // relaunch via shell open — Process.Start with the seam
        _cliRunner.Run("explorer.exe", "open");
    }

    private async Task ApplyDefaultsAsync()
    {
        // Confirm dialog: "The following settings will be changed to Vain defaults:"
        // + blank line + runtime list of changes
        var dialog = new ContentDialog
        {
            Title = "Vain Defaults",
            Content = $"The following settings will be changed to Vain defaults:\n\n" +
                      $"• Manual Windows Updates: enabled (SetDisableUXWUAccess = 0)\n" +
                      $"• Print Service: automatic start\n" +
                      $"• Game Bar: disabled (all 8 values cleared)\n" +
                      $"• Push Notifications: disabled (ConsentStore=Deny, ToastEnabled=0)\n" +
                      $"• Date & Time: unchanged",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            XamlRoot = _xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        // Reset the four segmented VMs to index 1 (Enabled)
        ManualUpdates.SelectedIndex = 1;
        PrintService.SelectedIndex = 1;
        GameBar.SelectedIndex = 1;
        PushNotifications.SelectedIndex = 1;

        // Backend writes via each VM's ApplyAsync
        await ManualUpdates.ApplyAsync(1);
        await PrintService.ApplyAsync(1);
        await GameBar.ApplyAsync(1);
        await PushNotifications.ApplyAsync(1);
    }

    private async Task RefreshAsync()
    {
        // Refresh re-reads all tab state via each VM's LoadAsync
        await LoadAsync();
    }
}