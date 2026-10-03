using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>General Windows settings overview.</summary>
public sealed partial class GeneralPage : Page
{
    public GeneralPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("General", TweakCatalog.All);
    }
}
