using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.Powereditor;

public sealed partial class PowereditorPage : Page
{
    public PowereditorPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PowerEditorViewModel>();
    }
}
