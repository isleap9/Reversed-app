using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.Services;
using VainTools.App.ViewModels;

namespace VainTools.App.Views;

public sealed partial class HomePage : Page
{
    /// <summary>Localized string accessor used by x:Bind function bindings.</summary>
    public LocalizedStrings Strings { get; }

    public HomePage(HomeViewModel viewModel)
    {
        Strings = App.Services.GetRequiredService<LocalizedStrings>();
        InitializeComponent();
        DataContext = viewModel;
    }
}
