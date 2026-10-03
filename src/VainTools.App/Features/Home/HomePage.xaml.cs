using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Home;

/// <summary>Landing page: system summary and quick actions.</summary>
public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
    }

    /// <summary>
    /// Quick-action tiles live inside a DataTemplate scoped to
    /// <see cref="QuickAction"/>, so the page's ViewModel is not reachable from
    /// <c>x:Bind</c> there. The tile carries the target type name in its Tag and
    /// the page forwards it to the ViewModel.
    /// </summary>
    private void OnQuickActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string typeName })
        {
            ViewModel.NavigateTo(typeName);
        }
    }
}
