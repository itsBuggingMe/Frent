using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>")]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |")]
[Variadic("out T comp", "|out T$ comp$, |")]
[Variadic("        comp = Entity.GetComp<T>(ref entityLocation);", "|        comp$ = Entity.GetComp<T$>(ref entityLocation);|")]
public static partial class EntityExtensions
{
    //extension class b/c the generator doesnt work when there are other attributes
    //and im too lazy to fix it
    public static void Deconstruct<T>(ref this Entity e, out Ref<T> comp)
    {
        if (!e.IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(Entity.EntityIsDeadMessage);

        comp = Entity.GetComp<T>(ref entityLocation);
    }

    public static void Deconstruct<T>(ref this Entity e, out T comp)
    {
        if (!e.IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(Entity.EntityIsDeadMessage);

        comp = Entity.GetComp<T>(ref entityLocation);
    }
}
