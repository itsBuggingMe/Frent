using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

/// <summary>
/// Deconstruction extensions for entities.
/// </summary>
[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>", 8)]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |", 8)]
[Variadic("        comp = Entity.GetComp<T>(ref entityLocation, components);", "|        comp$ = Entity.GetComp<T$>(ref entityLocation, components);|", 8)]
public static partial class EntityExtensions
{
    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s).
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have all the components specified.</exception>
    public static void Deconstruct<T>(ref this Entity e, out Ref<T> comp)
    {
        e.AssertIsAlive(out var world, out var entityLocation);

        IComponentRunner[] components = entityLocation.Archetype(world).Components;

        comp = Entity.GetComp<T>(ref entityLocation, components);
    }
}
