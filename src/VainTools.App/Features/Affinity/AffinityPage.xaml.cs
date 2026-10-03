using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Affinity;

/// <summary>CPU affinity and ideal-processor sets for running processes.</summary>
public sealed partial class AffinityPage : Page
{
    public AffinityPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AffinityViewModel>();
    }
}
