using System;
using System.Collections.Generic;

namespace VainTools.Core;

/// <summary>
/// Simple service container for dependency injection.
/// </summary>
public class ServiceProvider : IServiceFactory
{
    private readonly Dictionary<Type, Func<object>> _singletons = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    /// <summary>
    /// Registers a singleton service.
    /// </summary>
    public void RegisterSingleton<T>(Func<T> factory)
    {
        _singletons[typeof(T)] = factory;
    }

    /// <summary>
    /// Registers a scoped/transient service.
    /// </summary>
    public void RegisterTransient<T>(Func<T> factory)
    {
        _factories[typeof(T)] = factory;
    }

    /// <summary>
    /// Registers a service factory for the specified type.
    /// </summary>
    public void Register<T>(Func<T> factory)
    {
        RegisterTransient(factory);
    }

    /// <summary>
    /// Gets an instance of the specified service.
    /// </summary>
    public T GetService<T>()
    {
        var type = typeof(T);

        // Check singletons first
        if (_singletons.TryGetValue(type, out var singletonFactory))
        {
            return (T)singletonFactory();
        }

        // Check factories
        if (_factories.TryGetValue(type, out var factory))
        {
            return (T)factory();
        }

        throw new InvalidOperationException($"Service type {type.Name} is not registered.");
    }

    /// <summary>
    /// Gets a required instance of the specified service.
    /// </summary>
    public T GetRequiredService<T>() => GetService<T>();
}