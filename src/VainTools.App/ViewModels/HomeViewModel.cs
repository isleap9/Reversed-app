using CommunityToolkit.Mvvm.Input;
using VainTools.App.Views;
using VainTools.Framework.Navigation;
using VainTools.Framework.Services;
using VainTools.Framework.ViewModels;

namespace VainTools.App.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    public HomeViewModel(INavigationService navigation, IDialogService dialogs)
    {
        _navigation = navigation;
        _dialogs = dialogs;
        Title = "Home";
    }

    [RelayCommand]
    private void OpenSettings() => _navigation.NavigateTo<SettingsPage>();

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _dialogs.ShowInfoAsync(
            $"About {App.AppName}",
            $"{App.AppName}\nVersion {App.AppVersion}\n\nSettings are stored in:\n{App.SettingsFilePath}");
    }
}
