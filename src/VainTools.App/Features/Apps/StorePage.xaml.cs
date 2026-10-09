using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Apps;

/// <summary>Search installable apps via winget and install one.</summary>
public sealed partial class StorePage : Page
{
    public StoreViewModel ViewModel { get; }

    public StorePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<StoreViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Deliberately no search here: winget is only invoked once the user asks for a
        // query, so navigating to the page costs nothing and cannot fail on a machine
        // where winget is missing.
    }

    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            _ = ViewModel.SearchCommand.ExecuteAsync(null);
        }
    }
}
