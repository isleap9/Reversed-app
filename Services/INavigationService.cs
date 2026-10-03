using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace VainTools.Services;

/// <summary>
/// Interface for navigation between pages.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    /// <typeparam name="TPage">The page type to navigate to.</typeparam>
    Task NavigateToAsync<TPage>() where TPage : Page, new();

    /// <summary>
    /// Navigates to a named page.
    /// </summary>
    /// <param name="pageName">The page name (tag from NavigationView).</param>
    Task NavigateToAsync(string pageName);

    /// <summary>
    /// Goes back to the previous page.
    /// </summary>
    Task<bool> GoBackAsync();

    /// <summary>
    /// Gets whether navigation back is possible.
    /// </summary>
    Task<bool> CanGoBackAsync();

    /// <summary>
    /// Sets the navigation container.
    /// </summary>
    /// <param name="frame">The frame to use for navigation.</param>
    void SetNavigationContainer(Frame frame);
}