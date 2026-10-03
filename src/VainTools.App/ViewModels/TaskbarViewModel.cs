using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class TaskbarViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsTaskbarHidden { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    public TaskbarViewModel()
    {
        Title = "Taskbar";
    }
}