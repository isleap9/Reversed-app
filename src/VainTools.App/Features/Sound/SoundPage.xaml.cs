using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
}
