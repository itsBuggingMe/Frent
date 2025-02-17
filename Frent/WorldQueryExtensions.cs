using System.Runtime.InteropServices;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("<T>", "<|T$, |>")]
[Variadic("default(T).Rule", "|default(T$).Rule, |")]
[Variadic("        where T : struct, IRuleProvider", "|        where T$ : struct, IRuleProvider\n|")]
public static partial class WorldQueryExtensions
{
    //we could use static abstract methods IF NOT FOR DOTNET6
    /// <summary>
    /// Gets a query specified by the given rules
    /// </summary>
    /// <returns>The created or cached query.</returns>
    public static Query Query<T>(this World world)
        where T : struct, IRuleProvider
    {
        ref Query? cachedValue = ref CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashCache<T>.Value, out bool exists);
        if (!exists)
        {
            cachedValue = world.CreateQuery(MemoryHelpers.ReadOnlySpanToImmutableArray([default(T).Rule]));
        }
        return cachedValue!;
    }
}
