using Frent.Systems;
using Frent.Systems.Queries;
using Frent.Variadic.Generator;

namespace Frent;

/// <summary>
/// Extensions for building queries in a <see cref="World"/>.
/// </summary>
/// <variadic />
[Variadic("Query<T>", "Query<|T$, |>", 8)]
[Variadic(".With<T>()", "|.With<T$>()|")]
public static partial class WorldQueryExtensions
{
    /// <summary>
    /// Creates a query that includes all entities with the specified component(s).
    /// </summary>
    public static Query Query<T>(this World world) => new QueryBuilder(world).With<T>().Build();
}
