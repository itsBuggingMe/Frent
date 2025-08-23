using Frent.Collections;
using Frent.Core;
using Frent.Core.Structures;

namespace Frent.Updating;

internal class SingleComponentUpdateFilter : IComponentUpdateFilter
{
    // archetypical
    private ArchetypeComponentUpdateRecord[] _archetypes;
    // sparse
    private readonly ComponentSparseSetBase _componentSparseSet;
    

    private readonly ComponentBufferManager _bufferManager;
    private int _count;
    private readonly ComponentID _componentID;
    private readonly World _world;

    private bool IsArchetypical => _archetypes is not null;

    public SingleComponentUpdateFilter(World world, ComponentID component)
    {
        _world = world;
        _componentID = component;
        _bufferManager = Component.CachedComponentFactories[component.Type];

        if (!component.IsSparseComponent)
        {
            _archetypes = [];
            _componentSparseSet = null!;
            foreach (var archetype in world.EnabledArchetypes.AsSpan())
                ArchetypeAdded(archetype.Archetype(world)!);
        }
        else
        {
            _archetypes = null!;
            _componentSparseSet = world.WorldSparseSetTable[component.SparseIndex];
        }
    }


    public void Update()
    {
        var world = _world;
        var bufferManager = _bufferManager;

        if (IsArchetypical)
        {
            foreach (var (archetype, storage) in _archetypes.AsSpan(0, _count))
                bufferManager.RunArchetypical(archetype.Components.UnsafeArrayIndex(storage).Buffer, archetype, world);
        }
        else
        {
            bufferManager.RunSparse(_componentSparseSet, _world);
        }
    }

    public void ArchetypeAdded(Archetype archetype)
    {
        int index = archetype.GetComponentIndex(_componentID);
        if(index != 0)
        {
            MemoryHelpers.GetValueOrResize(ref _archetypes, _count++) = new(archetype, index);
        }
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes, ReadOnlySpan<int> ids)
    {
        var world = _world;
        if (IsArchetypical)
        {
            foreach ((var archetype, _, int initalEntityCount) in archetypes)
            {
                int componentIndex = archetype.GetComponentIndex(_componentID);
                if (componentIndex != 0)
                {//this archetype has this component type
                    archetype.Components[componentIndex].Run(archetype, world, initalEntityCount, archetype.EntityCount - initalEntityCount);
                }
            }
        }
        else
        {
            _bufferManager.RunSparse(_componentSparseSet, _world, ids);
        }
    }

    internal record struct ArchetypeComponentUpdateRecord(Archetype Archetype, nint ComponentStorageIndex);
}