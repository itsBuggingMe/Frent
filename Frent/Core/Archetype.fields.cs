using Frent.Buffers;
using Frent.Updating;

namespace Frent.Core;

partial class Archetype(ArchetypeID archetypeID, IComponentRunner[] components)
{
    //8
    internal IComponentRunner[] Components = components;
    //8
    private Chunk<Entity>[] _entities = [new Chunk<Entity>(1)];
    //8
    private ArchetypeID _archetypeID = archetypeID;
    private ushort _chunkIndex = 0;
    private int _componentIndex = 0;
    private int _chunkSize = 1;
}
