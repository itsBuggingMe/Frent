using Frent.Core.Structures;
using Frent.Updating;

namespace Frent.Core;

//38 bytes total - 16 header + mt, 8 comps, 8 create, 8 entities, 6 ids and tracking
partial class Archetype(ArchetypeID archetypeID, ComponentStorageBase[] components, ComponentStorageBase[] createBuffers)
{
    //8
    internal readonly ComponentStorageBase[] Components = components;
    //for speeeeeed reasons
    //when creating components during world updates
    //they are added to these arrays
    //these arrays should be heavily pooled to and fro
    internal readonly ComponentStorageBase[] CreateComponentBuffers = createBuffers;

    //8
    //we include version
    //this is so we dont need to lookup
    //the world table every time
    private EntityIDOnly[] _entities = new EntityIDOnly[1];

    //information for tag existence & component index per id
    //updated by static methods
    //saves a lookup on hot paths
    internal byte[] ComponentTagTable = GlobalWorldTables.ComponentTagLocationTable[archetypeID.RawIndex];
    //2
    private readonly ArchetypeID _archetypeID = archetypeID;
    //4
    /// <summary>
    /// The next component index
    /// </summary>
    private int _componentIndex = 0;
}