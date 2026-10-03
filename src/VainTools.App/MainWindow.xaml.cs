using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using VainTools.App.Features.Home;
using VainTools.Framework;
using VainTools.Framework.Messaging;
using VainTools.Framework.Navigation;
using VainTools.Framework.Services;
using CommunityToolkit.Mvvm.Messaging;
using Windows.Graphics;
using Windows.UI;

namespace VainTools.App;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Maps a nav item's <c>Tag</c> (a fully-qualified type name written in XAML) to the
    /// page type. XAML cannot express a <see cref="Type"/> in a Tag, and the
    /// <c>x:Type</c> markup extension is not available in WinUI, so the tree carries
    /// strings and this table resolves them against the assembly.
    /// </summary>
    private static readonly Dictionary<string, Type> PageTypesByTag =
        typeof(MainWindow).Assembly
            .GetTypes()
            .Where(t => t.Namespace is not null
                        && t.Namespace.StartsWith("VainTools.App.Features", StringComparison.Ordinal)
                        && typeof(Page).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t, StringComparer.Ordinal);

    private readonly INavigationService _navigation;
    private readonly IThemeService _theme;
    private readonly IMessenger _messenger;

    public MainWindow(
        INavigationService navigation,
        IInfoBarService infoBar,
        IThemeService theme,
        IMessenger messenger)
    {
        InitializeComponent();

        _navigation = navigation;
        InfoBar = infoBar;
        _theme = theme;
        _messenger = messenger;

        Title = App.AppName;

        SystemBackdrop = new MicaBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureWindow();

        _navigation.SetFrame(ContentFrame);
        _navigation.Navigated += (_, _) => RefreshShellState();

        // Land on Home, matching the real app's starting page.
        NavigateTo(typeof(HomePage));
        SelectNavItemFor(typeof(HomePage));

        _messenger.Register<ThemeChangedMessage>(this, (r, m) => ((MainWindow)r).ApplyTheme(m.Theme));
        _messenger.Register<NavigationRequestedMessage>(this, (r, m) =>
        {
            var window = (MainWindow)r;
            window.NavigateTo(m.PageType, m.Parameter);
            window.SelectNavItemFor(m.PageType);
        });
    }

    /// <summary>Global info-bar state bound by the shell.</summary>
    public IInfoBarService InfoBar { get; }

    /// <summary>App name shown in the custom title bar.</summary>
    public string AppTitle => App.AppName;

    /// <summary>App icon shown in the custom title bar.</summary>
    public ImageSource AppIconSource { get; } = LoadAppLogo();

    private static ImageSource LoadAppLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "AkariLogo.png");
        return File.Exists(path) ? new BitmapImage(new Uri(path)) : null!;
    }

    /// <summary>Applies an application theme to this window's content and title bar.</summary>
    public void ApplyTheme(AppTheme theme)
    {
        RootElement.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        ApplyTitleBarColors(theme);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Only leaf items (those carrying a Tag that maps to a page) navigate.
        // Group parents report themselves as SelectedItem when collapsed.
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        if (PageTypesByTag.TryGetValue(tag, out var pageType))
        {
            NavigateTo(pageType);
        }
    }

    private void NavigateTo(Type pageType, object? parameter = null)
    {
        if (pageType == _navigation.CurrentPageType)
        {
            return;
        }

        _navigation.NavigateTo(pageType, parameter);
    }

    /// <summary>
    /// Walks the nav tree and selects the item whose Tag maps to <paramref name="pageType"/>,
    /// expanding any parent group so the selection is visible.
    /// </summary>
    private void SelectNavItemFor(Type pageType)
    {
        var target = PageTypesByTag.FirstOrDefault(kv => kv.Value == pageType).Key;
        if (target is null)
        {
            return;
        }

        foreach (var root in EnumerateItems(NavView.MenuItems).Concat(EnumerateItems(NavView.FooterMenuItems)))
        {
            if (TrySelect(root, target))
            {
                return;
            }
        }
    }

    private bool TrySelect(NavigationViewItem item, string tag)
    {
        if (item.Tag as string == tag)
        {
            NavView.SelectedItem = item;
            return true;
        }

        foreach (var child in EnumerateItems(item.MenuItems))
        {
            if (TrySelect(child, tag))
            {
                item.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<NavigationViewItem> EnumerateItems(IEnumerable<object> items)
    {
        foreach (var entry in items)
        {
            if (entry is NavigationViewItem item)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Keeps the nav pane in sync when navigation happens from code rather than by
    /// user selection (e.g. a quick action on the Home page).
    /// </summary>
    private void RefreshShellState()
    {
        if (_navigation.CurrentPageType is { } current)
        {
            SelectNavItemFor(current);
        }
    }

    private void ConfigureWindow()
    {
        var appWindow = GetAppWindow();
        if (appWindow is null)
        {
            return;
        }

        try
        {
            var workArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            var width = Math.Min(1250, workArea.Width - 60);
            var height = Math.Min(860, workArea.Height - 80);
            appWindow.Resize(new SizeInt32((int)width, (int)height));

            appWindow.Move(new PointInt32(
                workArea.X + (workArea.Width - (int)width) / 2,
                workArea.Y + (workArea.Height - (int)height) / 2));

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AkariLogo.ico");
            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Window configuration failed: {ex.Message}");
        }

        ApplyTitleBarColors(_theme.CurrentTheme);
    }

    private AppWindow? GetAppWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void ApplyTitleBarColors(AppTheme theme)
    {
        var appWindow = GetAppWindow();
        if (appWindow?.TitleBar is null)
        {
            return;
        }

        var isDark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => RootElement.ActualTheme == ElementTheme.Dark,
        };

        var foreground = isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;
        var hoverBackground = isDark ? Microsoft.UI.Colors.Gray : Microsoft.UI.Colors.Transparent;

        appWindow.TitleBar.ForegroundColor = foreground;
        appWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonForegroundColor = foreground;
        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonHoverForegroundColor = foreground;
        appWindow.TitleBar.ButtonHoverBackgroundColor = hoverBackground;
        appWindow.TitleBar.ButtonPressedBackgroundColor = hoverBackground;
    }
}
