using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.App.Views;

public sealed partial class TaskbarPage : Page
{
    public ViewModels.TaskbarViewModel ViewModel { get; }

    public TaskbarPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ViewModels.TaskbarViewModel>();
        this.DataContext = ViewModel;
    }
}