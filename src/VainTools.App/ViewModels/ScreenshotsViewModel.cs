using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class ScreenshotsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _saveLocation = "";

    [ObservableProperty]
    private string _outputFormat = "PNG";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ScreenshotsViewModel()
    {
        Title = "Screenshots";
    }
}