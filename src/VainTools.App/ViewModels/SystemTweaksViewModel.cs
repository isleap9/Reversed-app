using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class SystemTweaksViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPerformanceModeEnabled;

    [ObservableProperty]
    private bool _isGameModeEnabled;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public SystemTweaksViewModel()
    {
        Title = "System Tweaks";
    }
}