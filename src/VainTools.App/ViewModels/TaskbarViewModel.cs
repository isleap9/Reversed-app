using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class TaskbarViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isTaskbarHidden;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public TaskbarViewModel()
    {
        Title = "Taskbar";
    }
}