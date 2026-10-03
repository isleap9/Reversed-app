using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VainTools.App.ViewModels;

namespace VainTools.App.Features.General;

/// <summary>Date, time and NTP server configuration.</summary>
public sealed partial class DateTimePage : Page
{
    public DateTimeViewModel ViewModel { get; }

    public DateTimePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DateTimeViewModel>();
    }
}
