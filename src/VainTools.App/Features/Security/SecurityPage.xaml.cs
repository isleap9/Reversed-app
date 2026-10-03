using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Security;

/// <summary>Security settings: Defender, VBS, driver blocklist and mitigations.</summary>
public sealed partial class SecurityPage : Page
{
    public SecurityPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("Security", TweakCatalog.Security);
    }
}