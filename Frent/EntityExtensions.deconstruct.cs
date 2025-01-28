using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

/// <summary>
/// Variadic extensions for entities
/// </summary>
[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>", 8)]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |", 8)]
[Variadic("out T comp", "|out T$ comp$, |", 8)]
[Variadic("        comp = Entity.GetComp<T>(ref entityLocation, components);", "|        comp$ = Entity.GetComp<T$>(ref entityLocation, components);|", 8)]
public static partial class EntityExtensions
{
    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s)
    /// </summary>
    /// <typeparam name="T">The component type to deconstruct</typeparam>
    /// <param name="e">The entity to deconstruct</param>
    /// <param name="comp">The reference to the entity's component of type <typeparamref name="T"/></param>
    /// <exception cref="InvalidOperationException">The entity is not alive</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have a component of type <typeparamref name="T"/></exception>
    public static void Deconstruct<T>(ref this Entity e, out Ref<T> comp)
    {
        e.AssertIsAlive(out var world, out var entityLocation);

        IComponentRunner[] components = entityLocation.Archetype(world).Components;

        comp = Entity.GetComp<T>(ref entityLocation, components);
    }

    /// <summary>
    /// Deconstructs the constituent components of an entity
    /// </summary>
    /// <param name="e">The entity to deconstruct</param>
    /// <typeparam name="T">The component type to deconstruct</typeparam>
    /// <param name="comp">The entity's component of type <typeparamref name="T"/></param>
    /// <exception cref="InvalidOperationException">The entity is not alive</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have a component of type <typeparamref name="T"/></exception>
    public static void Deconstruct<T>(ref this Entity e, out T comp)
    {
        e.AssertIsAlive(out var world, out var entityLocation);

        IComponentRunner[] components = entityLocation.Archetype(world).Components;

        comp = Entity.GetComp<T>(ref entityLocation, components);
    }
}
