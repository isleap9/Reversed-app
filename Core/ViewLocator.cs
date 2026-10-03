using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace VainTools.Core;

/// <summary>
/// Resolves Views and ViewModels by convention.
/// </summary>
public class ViewLocator
{
    private readonly ConcurrentDictionary<Type, Type> _viewViewModelPairs = new();

    /// <summary>
    /// Registers a View/ViewModel pair.
    /// </summary>
    public void RegisterPair(Type viewType, Type viewModelType)
    {
        _viewViewModelPairs.TryAdd(viewType, viewModelType);
    }

    /// <summary>
    /// Gets the ViewModel type for a View.
    /// </summary>
    public Type? GetViewModelForView(Type viewType)
    {
        if (_viewViewModelPairs.TryGetValue(viewType, out var viewModelType))
        {
            return viewModelType;
        }

        // Try conventional mapping
        var viewModelTypeFromConvention = GetConventionViewModel(viewType);
        if (viewModelTypeFromConvention != null)
        {
            _viewViewModelPairs.TryAdd(viewType, viewModelTypeFromConvention);
            return viewModelTypeFromConvention;
        }

        return null;
    }

    /// <summary>
    /// Gets the View type for a ViewModel.
    /// </summary>
    public Type? GetViewForViewModel(Type viewModelType)
    {
        var viewType = _viewViewModelPairs
            .Where(kvp => kvp.Value == viewModelType)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (viewType != null)
        {
            return viewType;
        }

        // Try conventional mapping
        var viewTypeFromConvention = GetConventionView(viewModelType);
        if (viewTypeFromConvention != null)
        {
            _viewViewModelPairs.TryAdd(viewTypeFromConvention, viewModelType);
            return viewTypeFromConvention;
        }

        return null;
    }

    private Type? GetConventionViewModel(Type viewType)
    {
        var viewModelName = viewType.FullName?.Replace("View", "ViewModel").Replace("Page", "ViewModel");
        if (string.IsNullOrEmpty(viewModelName))
            return null;

        return FindType(viewModelName) ?? viewType.Assembly.GetType(viewModelName);
    }

    private Type? GetConventionView(Type viewModelType)
    {
        var viewName = viewModelType.FullName?.Replace("ViewModel", "View").Replace("ViewModel", "Page");
        if (string.IsNullOrEmpty(viewName))
            return null;

        return FindType(viewName) ?? viewModelType.Assembly.GetType(viewName);
    }

    private Type? FindType(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }
        return null;
    }
}