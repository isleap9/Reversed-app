using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Startup;

/// <summary>Startup entries from Run keys and scheduled tasks.</summary>
public sealed partial class StartupPage : Page
{
    public StartupPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<StartupViewModel>();
    }

    private void OnRunKeyToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch { DataContext: StartupEntryViewModel entry })
        {
            _ = ((StartupViewModel)DataContext).ToggleRunKeyEntryCommand.ExecuteAsync(entry);
        }
    }

    private void OnScheduledTaskToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch { DataContext: StartupEntryViewModel entry })
        {
            _ = ((StartupViewModel)DataContext).ToggleScheduledTaskCommand.ExecuteAsync(entry);
        }
    }
}
