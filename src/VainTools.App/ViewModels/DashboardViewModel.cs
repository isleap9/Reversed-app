using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    public DashboardViewModel()
    {
        Title = "Dashboard";
    }
}