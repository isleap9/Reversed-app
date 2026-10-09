using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Apps;

/// <summary>Programs from the uninstall registry, with uninstall and copy-command.</summary>
public sealed partial class InstalledAppsPage : Page
{
    public InstalledAppsViewModel ViewModel { get; }

    public InstalledAppsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<InstalledAppsViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.RefreshCommand.ExecuteAsync(null);
    }
}
