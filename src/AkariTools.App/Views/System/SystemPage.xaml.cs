using AkariTools.App.ViewModels.General;
using AkariTools.App.ViewModels.System;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views.System;

public sealed partial class SystemPage : Page
{
    public SystemViewModel ViewModel => (SystemViewModel)DataContext;

    // Task 1–2 services consumed by later tasks (Task 5 Date & Time rebuild,
    // Task 6 segmented rows, Task 7 header actions). Stored now so this ctor
    // already carries the full System tab dependency set.
    private readonly IBcdService _bcdService;
    private readonly IDateFormatService _dateFormatService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ICliRunner _cliRunner;

    public SystemPage(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl,
        IBcdService bcdService,
        IDateFormatService dateFormatService,
        ITimeZoneService timeZoneService,
        ICliRunner cliRunner)
    {
        this.InitializeComponent();
        _bcdService = bcdService;
        _dateFormatService = dateFormatService;
        _timeZoneService = timeZoneService;
        _cliRunner = cliRunner;
        DataContext = new SystemViewModel(
            new ExplorerViewModel(registrySettingService),
            new ContextMenuViewModel(registrySettingService),
            new VisualViewModel(registrySettingService),
            new DateTimeViewModel(registrySettingService, serviceControl, dateFormatService, timeZoneService, cliRunner),
            new SettingsVisibilityViewModel(registrySettingService),
            new ManualUpdatesViewModel(registrySettingService),
            new PrintServiceViewModel(serviceControl),
            new GameBarViewModel(registrySettingService),
            new PushNotificationsViewModel(registrySettingService, serviceControl),
            cliRunner,
            this.XamlRoot);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        // OneWay binding: the row still holds the previous value when a user
        // toggle fires, so the switch value is passed explicitly. Revert
        // echoes (the switch snapping back after a failed apply) carry the
        // value already shown and are ignored.
        if (DataContext is not SystemViewModel system
            || sender is not ToggleSwitch toggle
            || toggle.DataContext is not SettingsPageRow row
            || toggle.IsOn == row.IsHidden)
            return;

        await system.SettingsVisibility.SetHiddenAsync(row, toggle.IsOn);
    }
}
