using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>Animations, shadows and visual effects.</summary>
public sealed partial class VisualPage : Page
{
    public VisualPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("Visual", TweakCatalog.Visual);
    }
}
