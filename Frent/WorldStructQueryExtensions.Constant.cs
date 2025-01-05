using Frent.Buffers;
using Frent.Systems;
using System.Runtime.InteropServices;

namespace Frent;

partial class WorldStructQueryExtensions
{
    //IQueryEntity
    //IQueryEntityUniform

    public static void InlineQuery<TQuery>(this World world, TQuery onEach)
        where TQuery : IQueryEntity
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes.Hash, out _) ??=
            world.CreateQuery([]);

        foreach (var archetype in query)
            ChunkHelpers.EnumerateChunkSpanEntity(archetype.CurrentWriteChunk, archetype.LastChunkComponentCount, onEach, archetype.GetEntitySpan());
    }

    public static void InlineQueryUniform<TQuery, TUniform>(this World world, TQuery onEach)
        where TQuery : IQueryEntityUniform<TUniform>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes.Hash, out _) ??=
            world.CreateQuery([]);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        foreach (var archetype in query)
            ChunkHelpers.EnumerateChunkSpanEntity(archetype.CurrentWriteChunk, archetype.LastChunkComponentCount, onEach, archetype.GetEntitySpan(), in uniform);
    }
}
