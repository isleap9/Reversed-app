using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace AkariTools.App.Controls;

/// <summary>
/// Shared segmented row: a two-segment <c>Disabled | Enabled</c> picker with a
/// label and an optional description. Consumers bind <see cref="Label"/>,
/// <see cref="Description"/>, <see cref="SelectedIndex"/> (one-way,
/// VM→control) and <see cref="IsApplying"/>, and set
/// <see cref="SelectCommand"/> to apply user picks (invoked with the picked
/// index, boxed as an <see cref="int"/>). Both segments disable while
/// <see cref="IsApplying"/> is true; programmatic
/// <see cref="SelectedIndex"/> changes (e.g. a revert after a failed apply)
/// still re-fire the command path, so consumers ignore no-op picks with a
/// same-value guard (see <c>SegmentedRowViewModel</c>).
/// </summary>
public sealed partial class SegmentedSettingRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(SegmentedSettingRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(SegmentedSettingRow),
            new PropertyMetadata(null, OnDescriptionChanged));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex), typeof(int), typeof(SegmentedSettingRow),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty IsApplyingProperty =
        DependencyProperty.Register(
            nameof(IsApplying), typeof(bool), typeof(SegmentedSettingRow),
            new PropertyMetadata(false, OnIsApplyingChanged));

    /// <summary>
    /// When set, user picks are routed here (with the picked segment index)
    /// instead of raising an event. Commands survive DataTemplates, where
    /// event handlers are unreliable in WinUI 3.
    /// </summary>
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register(
            nameof(SelectCommand), typeof(ICommand), typeof(SegmentedSettingRow),
            new PropertyMetadata(null));

    public SegmentedSettingRow()
    {
        this.InitializeComponent();
        UpdateSegments();
        UpdateSegmentsEnabled();
        UpdateDescriptionVisibility();
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

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public bool IsApplying
    {
        get => (bool)GetValue(IsApplyingProperty);
        set => SetValue(IsApplyingProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SegmentedSettingRow row)
            row.UpdateDescriptionVisibility();
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SegmentedSettingRow row)
            row.UpdateSegments();
    }

    private static void OnIsApplyingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SegmentedSettingRow row)
            row.UpdateSegmentsEnabled();
    }

    private void UpdateDescriptionVisibility()
    {
        if (DescriptionText is not null)
        {
            DescriptionText.Visibility = string.IsNullOrEmpty(Description)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void UpdateSegments()
    {
        if (DisabledSegment is null || EnabledSegment is null)
            return;
        if (SelectedIndex == 0)
            DisabledSegment.IsChecked = true;
        else if (SelectedIndex == 1)
            EnabledSegment.IsChecked = true;
    }

    private void UpdateSegmentsEnabled()
    {
        if (DisabledSegment is null || EnabledSegment is null)
            return;
        DisabledSegment.IsEnabled = !IsApplying;
        EnabledSegment.IsEnabled = !IsApplying;
    }

    private void Segment_Checked(object sender, RoutedEventArgs e)
    {
        if (SelectCommand is null)
            return;
        int index;
        if (ReferenceEquals(sender, DisabledSegment))
            index = 0;
        else if (ReferenceEquals(sender, EnabledSegment))
            index = 1;
        else
            return;
        SelectCommand.Execute(index);
    }
}
