namespace Frent.Components;

/// <summary>
/// Marks a component to have a <see cref="Destroy"/> method to be called at the end of a component lifetime.
/// </summary>
public interface IDestroyable
{
    /// <summary>
    /// This method is called whenever a component reaches the end of its lifetime, whether by an <see cref="Entity.Remove{T}()"/> method or <see cref="Entity.Delete"/>.
    /// </summary>
    void Destroy();
}