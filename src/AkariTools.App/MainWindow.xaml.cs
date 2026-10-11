using AkariTools.App.Helpers;
using AkariTools.App.Views;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App;

public sealed partial class MainWindow : Window
{
    private readonly IHardwareService _hardwareService;
    private readonly IThemeService _themeService;
    private readonly IRegistrySettingService _registrySettingService;
    private readonly IServiceControl _serviceControl;

    private readonly Dictionary<string, Page> _pageCache = new(StringComparer.Ordinal);
    private readonly Func<string, Page?> _pageFactory;

    public MainWindow(
        IHardwareService hardwareService,
        IThemeService themeService,
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl)
    {
        LaunchLog.Write("MainWindow ctor start");
        this.InitializeComponent();
        LaunchLog.Write("MainWindow InitializeComponent done");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _hardwareService = hardwareService;
        _themeService = themeService;
        _registrySettingService = registrySettingService;
        _serviceControl = serviceControl;

        _pageFactory = CreatePage;

        ApplyTheme(_themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
        Closed += (_, _) => _themeService.ThemeChanged -= OnThemeChanged;

        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(new HomePage(hardwareService, themeService));
        LaunchLog.Write("MainWindow HomePage added");
        if (NavView.MenuItems.Count > 0 && NavView.MenuItems[0] is NavigationViewItem first)
            NavView.SelectedItem = first;
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        DispatcherQueue.TryEnqueue(() => ApplyTheme(theme));

    private void ApplyTheme(AppTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                AppTheme.FullDark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    private Page? CreatePage(string tag) => tag switch
    {
        "Home" => new HomePage(_hardwareService, _themeService),
        // Task 4: construct GeneralPage here
        // Task 5: construct ExplorerPage here
        // Task 6: construct ContextMenuPage here
        // Task 7: construct VisualPage here
        // Task 8: construct DateTimePage here
        // Task 9: construct SettingsVisibilityPage here
        _ => null,
    };

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;
        if (item.Tag is not string tag || string.IsNullOrEmpty(tag))
            return;

        if (_pageCache.TryGetValue(tag, out var cached))
        {
            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(cached);
            return;
        }

        var page = _pageFactory(tag);
        if (page is null)
            return;

        _pageCache[tag] = page;
        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(page);
    }
}
