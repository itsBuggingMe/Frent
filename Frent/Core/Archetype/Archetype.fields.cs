using Frent.Core.Structures;
using Frent.Updating;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Frent.Core;

//46 bytes total - 16 header + mt, 8 comps, 8 entities, 8 table, 6 ids and tracking
partial class Archetype(ArchetypeID archetypeID, ComponentStorageRecord[] components, bool isTempCreateArchetype)
{
    //8
    internal readonly ComponentStorageRecord[] Components = components;

    //8
    //we include version
    //this is so we dont need to lookup
    //the world table every time
    private EntityIDOnly[] _entities = isTempCreateArchetype ? Array.Empty<EntityIDOnly>() : new EntityIDOnly[1];

    //8
    //information for tag existence & component index per id
    //updated by static methods
    //saves a lookup on hot paths
    internal byte[] ComponentTagTable = GlobalWorldTables.ComponentTagLocationTable[archetypeID.RawIndex];
    //2
    private readonly ArchetypeID _archetypeID = archetypeID;
    //4
    /// <summary>
    /// The next component index or deferred entity count
    /// </summary>
    /// <remarks>
    /// You can think of this as a discrimminated union. Next component index is the non-deferred count of a normal archetype. 
    /// Deferred entity count is the total number of deferred entities, some of which may be stored directly on the normal archetype.
    /// </remarks>
    private int _nextComponentIndexOrDeferredEntityCount = 0;

#if DEBUG
    private ref int NextComponentIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(!_isTempCreationArchetype, "NextComponentIndex called on non-temp creation archetype");
            return ref _nextComponentIndexOrDeferredEntityCount;
        }
    }

    private ref int DeferredEntityCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(_isTempCreationArchetype, "DeferredEntityCount called on temp creation archetype");
            return ref _nextComponentIndexOrDeferredEntityCount;
        }
    }

    private readonly bool _isTempCreationArchetype = isTempCreateArchetype;
#else
    private ref int NextComponentIndex => ref _nextComponentIndexOrDeferredEntityCount;
    private ref int DeferredEntityCount => ref _nextComponentIndexOrDeferredEntityCount;
#endif
}