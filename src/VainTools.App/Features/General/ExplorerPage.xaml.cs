using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>File Explorer tweaks: extensions, hidden files, Quick Access and autoplay.</summary>
public sealed partial class ExplorerPage : Page
{
    public ExplorerPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("Explorer", TweakCatalog.Explorer);
    }
}
