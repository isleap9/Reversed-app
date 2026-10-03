using CommunityToolkit.Mvvm.ComponentModel;
using VainTools.Framework;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class ScreenshotsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string SaveLocation { get; set; } = "";

    [ObservableProperty]
    public partial string OutputFormat { get; set; } = "PNG";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    public ScreenshotsViewModel()
    {
        Title = "Screenshots";
    }
}