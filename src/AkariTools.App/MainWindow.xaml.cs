using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = "Akari Toolbox";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // HomePage does not exist yet (Task 6); select Home and leave placeholder content.
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // TODO: Page navigation in future tasks.
    }
}
