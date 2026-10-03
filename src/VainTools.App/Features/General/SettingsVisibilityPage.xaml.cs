using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>Show or hide pages in the Windows Settings app.</summary>
public sealed partial class SettingsVisibilityPage : Page
{
    public SettingsVisibilityViewModel ViewModel { get; }

    public SettingsVisibilityPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsVisibilityViewModel>();
    }
}
