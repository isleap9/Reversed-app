using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using VainTools.App.Services;
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

    // A ToggleSwitch bound to an immutable record also fires Toggled when the binding
    // pushes a value into it. Acting on that echo makes Refresh → rebind → Toggled →
    // Toggle recurse without end, so only a toggle that actually differs from the model
    // is treated as a user action.
    private void OnRunKeyToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch { DataContext: StartupEntry entry } toggle ||
            DataContext is not StartupViewModel vm ||
            !StartupViewModel.IsUserToggle(entry, toggle.IsOn))
        {
            return;
        }

        _ = vm.ToggleRunKeyEntryCommand.ExecuteAsync(entry);
    }

    private void OnScheduledTaskToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch { DataContext: StartupEntry entry } toggle ||
            DataContext is not StartupViewModel vm ||
            !StartupViewModel.IsUserToggle(entry, toggle.IsOn))
        {
            return;
        }

        _ = vm.ToggleScheduledTaskCommand.ExecuteAsync(entry);
    }
}
