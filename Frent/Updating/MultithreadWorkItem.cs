using static Frent.Updating.UpdateFilter;

namespace Frent.Updating;

// can be chunk of an archetype
// a collection of archetype chunks
// or a collection of sparse components
internal readonly struct MultithreadWorkItem
{
    public readonly MultithreadWorkItemType Type;

    public readonly ArchetypeUpdateSpan Chunk;
    public readonly int ChunkStart;
    public readonly int ChunkLength;

    public readonly Stack<ArchetypeUpdateSpan>? ChunkCollection;
    public readonly ArraySegment<SparseUpdateMethod> SparseUpdateMethods;

    public MultithreadWorkItem(ArchetypeUpdateSpan chunk, int start, int length)
    {
        Type = MultithreadWorkItemType.Chunk;
        Chunk = chunk;
        ChunkStart = start;
        ChunkLength = length;
    }

    public MultithreadWorkItem(Stack<ArchetypeUpdateSpan> chunkCollection)
    {
        Type = MultithreadWorkItemType.ChunkCollection;
        ChunkCollection = chunkCollection;
    }

    public MultithreadWorkItem(ArraySegment<SparseUpdateMethod> sparseUpdateMethods)
    {
        Type = MultithreadWorkItemType.SparseComponentCollection;
        SparseUpdateMethods = sparseUpdateMethods;
    }
}

internal enum MultithreadWorkItemType
{
    Chunk,
    ChunkCollection,
    SparseComponentCollection
}