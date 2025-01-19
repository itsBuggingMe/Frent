using Frent.Buffers;
using Frent.Updating;

namespace Frent.Core;

//44 bytes total - 16 header + mt, 8 comps, 8 entities, 12 ids and tracking
partial class Archetype(ArchetypeID archetypeID, IComponentRunner[] components)
{
    //8
    internal IComponentRunner[] Components = components;
    //8
    private Chunk<Entity>[] _entities = [new Chunk<Entity>(1)];
    //4
    private ArchetypeID _archetypeID = archetypeID;
    private ushort _chunkIndex = 0;
    //8
    private int _componentIndex = 0;
    private int _chunkSize = 1;
}
