using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>")]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |")]
[Variadic("out T comp", "|out T$ comp$, |")]
[Variadic("        comp = Entity.GetComp<T>(ref entityLocation);", "|        comp$ = Entity.GetComp<T$>(ref entityLocation);|")]
[Variadic("    /// <typeparam name=\"T\">The component type to deconstruct</typeparam>",
    "|    /// <typeparam name=\"T$\">Component type number $ to deconstruct</typeparam>\n|")]
[Variadic("    /// <param name=\"comp\">The reference to the entity's component of type <typeparamref name=\"T\"/></param>",
    "|    /// <param name=\"comp$\">The reference to the entity's component of type <typeparamref name=\"T$\"/></param>\n|")]
[Variadic("    /// <param name=\"comp\">The entity's component of type <typeparamref name=\"T\"/></param>",
    "|    /// <param name=\"comp$\">The entity's component of type <typeparamref name=\"T$\"/></param>\n|")]
public static partial class EntityExtensions
{
    //extension class b/c the generator doesn't work when there are other attributes
    //and im too lazy to fix it

    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s)
    /// </summary>
    /// <typeparam name="T">The component type to deconstruct</typeparam>
    /// <param name="comp">The reference to the entity's component of type <typeparamref name="T"/></param>
    /// <exception cref="InvalidOperationException">The entity is not alive</exception>
    /// <exception cref="ComponentNotFoundException{T}">The entity does not have a component of type <typeparamref name="T"/></exception>
    public static void Deconstruct<T>(ref this Entity e, out Ref<T> comp)
    {
        if (!e.IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(Entity.EntityIsDeadMessage);

        comp = Entity.GetComp<T>(ref entityLocation);
    }

    /// <summary>
    /// Deconstructs the constituent components of an entity
    /// </summary>
    /// <typeparam name="T">The component type to deconstruct</typeparam>
    /// <param name="comp">The entity's component of type <typeparamref name="T"/></param>
    /// <exception cref="InvalidOperationException">The entity is not alive</exception>
    /// <exception cref="ComponentNotFoundException{T}">The entity does not have a component of type <typeparamref name="T"/></exception>
    public static void Deconstruct<T>(ref this Entity e, out T comp)
    {
        if (!e.IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(Entity.EntityIsDeadMessage);

        comp = Entity.GetComp<T>(ref entityLocation);
    }
}
