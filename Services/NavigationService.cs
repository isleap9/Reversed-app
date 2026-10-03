using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VainTools.Services;

/// <summary>
/// Service for navigating between pages.
/// </summary>
public class NavigationService : INavigationService
{
    private Frame? _frame;

    public void SetNavigationContainer(Frame frame)
    {
        _frame = frame;
    }

    public async Task NavigateToAsync<TPage>() where TPage : Page, new()
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation container not set.");
        }

        await Task.Run(() => _frame.Navigate(new TPage()));
    }

    public async Task NavigateToAsync(string pageName)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation container not set.");
        }

        // Map page names to page types
        // This will be expanded as pages are created
        await Task.CompletedTask;
    }

    public async Task<bool> GoBackAsync()
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation container not set.");
        }

        await Task.Run(() => { if (_frame.CanGoBack) _frame.GoBack(); });
        return _frame?.CanGoBack == true;
    }

    public async Task<bool> CanGoBackAsync()
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation container not set.");
        }

        return await Task.FromResult(_frame.CanGoBack);
    }
}