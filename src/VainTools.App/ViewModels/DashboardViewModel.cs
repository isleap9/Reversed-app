using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    public DashboardViewModel()
    {
        Title = "Dashboard";
    }
}