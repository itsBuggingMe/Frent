using Frent.Components;

namespace Frent;

/// <summary>
/// Defines a uniform provider, which is used by <see cref="World"/> to supply uniforms to components and queries
/// e.g., <see cref="IUniformComponent{TUniform}"/>
/// </summary>
public interface IUniformProvider
{
    /// <summary>
    /// Gets a uniform from this uniform provider
    /// </summary>
    /// <typeparam name="T">The type of uniform to retrieve</typeparam>
    /// <returns>The uniform</returns>
    T GetUniform<T>();
}
