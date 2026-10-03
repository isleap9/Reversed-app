using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class SystemTweaksViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsPerformanceModeEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsGameModeEnabled { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    public SystemTweaksViewModel()
    {
        Title = "System Tweaks";
    }
}