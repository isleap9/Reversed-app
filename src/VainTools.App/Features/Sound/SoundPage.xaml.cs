using Microsoft.Extensions.DependencyInjection;
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
}
