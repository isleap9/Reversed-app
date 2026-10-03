using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>Windows context-menu and shell integration.</summary>
public sealed partial class ContextMenuPage : Page
{
    public ContextMenuPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("Context Menu", TweakCatalog.ContextMenu);
    }
}
