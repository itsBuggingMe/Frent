namespace Frent.Components;

/// <summary>
/// Marks a component to have a <see cref="Init(Entity)"/> method to be called at the start of a component lifetime.
/// </summary>
public interface IInitable
{
    /// <summary>
    /// This method is called whenever a component begins its lifetime, whether by any <see cref="Entity.Add{T}(in T)"/> method or any <see cref="World.Create{T}(in T)"/> method (but not <see cref="World.CreateMany{T}(int)"/>).
    /// </summary>
    /// <param name="self">The <see cref="Entity"/> this component belongs to.</param>
    void Init(Entity self);
}