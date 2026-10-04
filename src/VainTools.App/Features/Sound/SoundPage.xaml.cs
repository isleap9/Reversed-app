using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Sound;

/// <summary>Audio devices, volume mixer, spatial audio and enhancements.</summary>
public sealed partial class SoundPage : Page
{
    public SoundPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SoundPageViewModel>();
    }

    // The enhancement toggles use a plain {Binding} (the rows are created by the view
    // model, so they cannot be x:Bind'd). A TwoWay binding pushes the control's initial
    // state into the view model after the page renders, so writing on every IsOn change
    // made simply opening this page rewrite the registry. Only a real user flip is acted on.
    private void OnEnhancementToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch { DataContext: TweakToggleViewModel row })
        {
            row.ApplyToggle(row.IsOn);
        }
    }

    // The mixer controls are unguarded WinUI inputs: Slider.ValueChanged and
    // ToggleSwitch.Toggled also fire on programmatic binding updates, and
    // ComboBox.SelectionChanged fires when the device list is rebuilt. Every
    // handler below ignores values that match the last system-read state.
    private void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo ||
            DataContext is not SoundPageViewModel vm ||
            combo.SelectedItem is not AudioDevice device ||
            Equals(vm.SelectedDevice, device))
        {
            return;
        }

        _ = vm.SelectDeviceCommand.ExecuteAsync(device);
    }

    private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not SoundPageViewModel vm ||
            vm.SelectedDevice is null ||
            !vm.IsUserVolumeChange(e.NewValue))
        {
            return;
        }

        _ = vm.SetVolumeAsync(vm.SelectedDevice.Id, (float)(e.NewValue / 100));
    }

    private void OnMuteToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle ||
            DataContext is not SoundPageViewModel vm ||
            vm.SelectedDevice is null ||
            !vm.IsUserMuteChange(toggle.IsOn))
        {
            return;
        }

        _ = vm.SetMuteAsync(vm.SelectedDevice.Id, toggle.IsOn);
    }
}
