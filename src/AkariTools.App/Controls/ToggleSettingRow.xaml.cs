using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace AkariTools.App.Controls;

/// <summary>
/// Shared toggle row: a <see cref="ToggleSwitch"/> with a label (header) and an
/// optional description. Consumers bind <see cref="Label"/>, <see cref="Description"/>,
/// <see cref="IsOn"/> (one-way, VM→control) and <see cref="IsApplying"/>, and
/// either handle <see cref="Toggled"/> or set <see cref="ToggleCommand"/> to
/// apply user toggles. The switch disables while <see cref="IsApplying"/> is
/// true; programmatic <see cref="IsOn"/> changes (e.g. a revert after a failed
/// apply) still raise <see cref="Toggled"/> so consumers can ignore no-op
/// toggles.
/// </summary>
public sealed partial class ToggleSettingRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(ToggleSettingRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(ToggleSettingRow),
            new PropertyMetadata(null, OnDescriptionChanged));

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(
            nameof(IsOn), typeof(bool), typeof(ToggleSettingRow),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsApplyingProperty =
        DependencyProperty.Register(
            nameof(IsApplying), typeof(bool), typeof(ToggleSettingRow),
            new PropertyMetadata(false, OnIsApplyingChanged));

    /// <summary>
    /// When set, user toggles are routed here (with the switch's new value)
    /// instead of raising <see cref="Toggled"/>. Commands survive
    /// DataTemplates, where event handlers are unreliable in WinUI 3.
    /// </summary>
    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(
            nameof(ToggleCommand), typeof(ICommand), typeof(ToggleSettingRow),
            new PropertyMetadata(null));

    /// <summary>
    /// Raised with the new value whenever the switch moves, including
    /// programmatic reverts (consumers compare against their VM state to
    /// ignore no-op toggles).
    /// </summary>
    public event EventHandler<bool>? Toggled;

    public ToggleSettingRow()
    {
        this.InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public bool IsApplying
    {
        get => (bool)GetValue(IsApplyingProperty);
        set => SetValue(IsApplyingProperty, value);
    }

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSettingRow row && row.DescriptionText is not null)
        {
            row.DescriptionText.Visibility = string.IsNullOrEmpty(e.NewValue as string)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private static void OnIsApplyingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSettingRow row && row.Switch is not null)
            row.Switch.IsEnabled = !(bool)e.NewValue;
    }

    private void Switch_Toggled(object sender, RoutedEventArgs e)
    {
        // Read from the switch, not the DP: the x:Bind source update may lag the event.
        if (sender is not ToggleSwitch toggle)
            return;
        if (ToggleCommand is not null)
        {
            ToggleCommand.Execute(toggle.IsOn);
            return;
        }
        Toggled?.Invoke(this, toggle.IsOn);
    }
}
