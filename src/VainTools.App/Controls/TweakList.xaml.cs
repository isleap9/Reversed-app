using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Controls;

/// <summary>
/// Shared presenter for the registry-tweak pages.
///
/// Pages set <see cref="ViewModel"/> and the title/subtitle, and the control handles
/// the list, the elevation notice and the Explorer-restart action. This keeps the six
/// General/System pages from each reimplementing the same markup.
/// </summary>
public sealed partial class TweakList : UserControl
{
    public TweakList()
    {
        InitializeComponent();
    }

    /// <summary>The page's tweak view model. Setting it wires up the list.</summary>
    public TweakPageViewModel? ViewModel
    {
        get => (TweakPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(TweakPageViewModel),
            typeof(TweakList),
            new PropertyMetadata(null, OnViewModelChanged));

    /// <summary>Page heading.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(TweakList),
            new PropertyMetadata(string.Empty, (d, e) =>
                ((TweakList)d).TitleText.Text = (string)e.NewValue));

    /// <summary>One-line explanation under the heading.</summary>
    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle), typeof(string), typeof(TweakList),
            new PropertyMetadata(string.Empty, (d, e) =>
                ((TweakList)d).SubtitleText.Text = (string)e.NewValue));

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TweakList control || e.NewValue is not TweakPageViewModel vm)
        {
            return;
        }

        control.TweaksHost.ItemsSource = vm.Tweaks;
        control.ElevationBar.IsOpen = vm.ShowElevationNotice;
        control.RestartExplorerButton.Visibility =
            vm.HasExplorerTweaks ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => ViewModel?.Refresh();

    private void OnRestartExplorerClick(object sender, RoutedEventArgs e)
        => _ = ViewModel?.RestartExplorerAsync();
}
