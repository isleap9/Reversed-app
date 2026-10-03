using System;

namespace VainTools.Core;

/// <summary>
/// Factory interface for service registration and resolution.
/// </summary>
public interface IServiceFactory
{
    /// <summary>
    /// Registers a service factory for the specified type.
    /// </summary>
    void Register<T>(Func<T> factory);

    /// <summary>
    /// Gets an instance of the specified service.
    /// </summary>
    T GetService<T>();

    /// <summary>
    /// Gets a required instance of the specified service.
    /// </summary>
    T GetRequiredService<T>();
}