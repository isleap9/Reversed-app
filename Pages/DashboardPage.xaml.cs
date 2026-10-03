using Microsoft.UI.Xaml.Controls;
using VainTools.Modules;

namespace VainTools.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        this.InitializeComponent();
        UpdateGpuInfo();
    }

    private void UpdateGpuInfo()
    {
        var settings = GovernorModule.GetCurrentSettings();
        GpuInfoText.Text = $"GPU: NVIDIA GeForce RTX 4090\nTemp: {settings.GetValueOrDefault("gpuTemperature", 0)}°C\nFan: {settings.GetValueOrDefault("nvFanSpeed", 0)}%";
    }
}