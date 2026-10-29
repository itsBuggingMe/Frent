using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Frent.Systems;

/// <summary>
/// Represents a set of entities from a world which can have systems applied to
/// </summary>
public partial class Query
{
    internal readonly QueryImpl Impl;
    internal Query(QueryImpl impl)
    {
        Impl = impl;
    }

    /// <summary>
    /// Enumerates <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    public EntityQueryEnumerator.Enumerable Entities => new(this);
}

/// <variadic />
[Variadic("<T>", "<|T$, |>")]
partial class Query
{
    /// <summary>
    /// Enumerates component references for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public QueryEnumerator<T>.Enumerable Enumerate<T>() => new(this);
    /// <summary>
    /// Enumerates component references and <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public EntityQueryEnumerator<T>.Enumerable EnumerateWithEntities<T>() => new(this);
    /// <summary>
    /// Enumerates component chunks for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public ChunkQueryEnumerator<T>.Enumerable EnumerateChunks<T>() => new(this);
}

partial class Query
{
    /// <summary>
    /// Enumerates <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    public EntityQueryEnumerator.Enumerable EnumerateWithEntities() => new(this);
}