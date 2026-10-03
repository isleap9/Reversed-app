using CommunityToolkit.Mvvm.ComponentModel;

namespace VainTools.App.ViewModels;

/// <summary>
/// Represents a single CPU in the affinity editor checkbox matrix.
/// </summary>
public partial class CpuAffinityViewModel : ObservableObject
{
    [ObservableProperty] public partial int Index { get; set; }
    [ObservableProperty] public partial int CoreIndex { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; }

    public CpuAffinityViewModel(int index, int coreIndex, bool isEnabled)
    {
        Index = index;
        CoreIndex = coreIndex;
        IsEnabled = isEnabled;
    }
}
