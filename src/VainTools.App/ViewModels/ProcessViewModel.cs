using CommunityToolkit.Mvvm.ComponentModel;

namespace VainTools.App.ViewModels;

/// <summary>
/// Represents a running process in the affinity editor.
/// </summary>
public partial class ProcessViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial int CpuCount { get; set; }

    public ProcessViewModel(int id, string name, int cpuCount)
    {
        Id = id;
        Name = name;
        CpuCount = cpuCount;
    }
}
