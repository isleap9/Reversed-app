using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.System;

/// <summary>System-level tweaks: startup sound, transparency and notifications.</summary>
public sealed partial class SystemPage : Page
{
    public SystemPage()
    {
        InitializeComponent();
        List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
            .Create("System", TweakCatalog.System);
    }
}
