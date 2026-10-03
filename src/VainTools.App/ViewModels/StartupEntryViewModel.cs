using CommunityToolkit.Mvvm.ComponentModel;

namespace VainTools.App.ViewModels;

/// <summary>
/// Represents a startup entry from a Run key or scheduled task.
/// </summary>
public partial class StartupEntryViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Command { get; set; }
    [ObservableProperty] public partial string Source { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; }

    public StartupEntryViewModel(string name, string command, string source, bool isEnabled)
    {
        Name = name;
        Command = command;
        Source = source;
        IsEnabled = isEnabled;
    }
}
