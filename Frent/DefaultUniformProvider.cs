using CommunityToolkit.HighPerformance.Helpers;
using Frent.Systems;
using System.Runtime.CompilerServices;

namespace Frent;

/// <summary>
/// The default uniform provider, using a dictionary.
/// </summary>
public class DefaultUniformProvider : IUniformProvider
{
    private Dictionary<Type, object> _uniforms = [];

    /// <summary>
    /// Adds a uniform to this uniform provider.
    /// </summary>
    /// <typeparam name="T">The type of uniform to add.</typeparam>
    /// <param name="obj">The object to add as a uniform.</param>
    /// <returns>This instance, for method chaining.</returns>
    public DefaultUniformProvider Add<T>(T obj)
        where T : notnull
    {
        object boxed = obj;
#if NETSTANDARD2_1
        if (boxed is null)
            throw new ArgumentNullException(nameof(obj));
#else
        ArgumentNullException.ThrowIfNull(boxed);
#endif
        _uniforms[typeof(T)] = boxed;
        return this;
    }

    /// <summary>
    /// Adds a uniform to this uniform provider.
    /// </summary>
    /// <param name="type">The type of uniform to add as.</param>
    /// <param name="object">The object to add as a uniform.</param>
    /// <returns>This instance, for method chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="object"/> is not assignable to <paramref name="type"/>.</exception>
    public DefaultUniformProvider Add(Type type, object @object)
    {
#if NETSTANDARD2_1
        if (type is null)
            throw new ArgumentNullException(nameof(type));
#else
        ArgumentNullException.ThrowIfNull(type);
#endif
        if (!type.IsAssignableFrom(@object.GetType()))
            throw new ArgumentException("Object must be assignable to the type!", nameof(@object));
        _uniforms[type] = @object;
        return this;
    }

    /// <summary>
    /// Gets a uniform from this default uniform provider.
    /// </summary>
    /// <typeparam name="T">The type of uniform to get.</typeparam>
    /// <returns>The uniform instance.</returns>
    /// <exception cref="InvalidOperationException">The uniform of the specified type is not found.</exception>
    public T GetUniform<T>() => _uniforms.TryGetValue(typeof(T), out object? value) ?
        (T)value :
        throw new InvalidOperationException($"Uniform of {typeof(T).Name} not found");
}