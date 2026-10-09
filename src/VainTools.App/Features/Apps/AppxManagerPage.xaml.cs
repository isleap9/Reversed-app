using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Apps;

/// <summary>Installed and provisioned Appx package management.</summary>
public sealed partial class AppxManagerPage : Page
{
    public AppxManagerViewModel ViewModel { get; }

    public AppxManagerPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AppxManagerViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.RefreshCommand.ExecuteAsync(null);
    }
}
