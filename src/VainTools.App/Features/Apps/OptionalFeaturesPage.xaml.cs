using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Apps;

/// <summary>Windows optional feature enable/disable.</summary>
public sealed partial class OptionalFeaturesPage : Page
{
    public OptionalFeaturesViewModel ViewModel { get; }

    public OptionalFeaturesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OptionalFeaturesViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.RefreshCommand.ExecuteAsync(null);
    }
}
