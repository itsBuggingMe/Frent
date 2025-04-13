using Frent.Core;

namespace Frent.Updating;

internal class SingleComponentUpdateFilter : IComponentUpdateFilter
{
    private (Archetype Archetype, ComponentStorageBase Storage)[] _archetypes = [];
    private int _count;
    private readonly ComponentID _componentID;
    private readonly World _world;

    public SingleComponentUpdateFilter(World world, ComponentID component)
    {
        _world = world;
        _componentID = component;

        foreach (var archetype in world.EnabledArchetypes.AsSpan())
            ArchetypeAdded(archetype.Archetype(world)!);
    }


    public void Update()
    {
        var world = _world;
        foreach(var (archetype, storage) in _archetypes)
            storage.Run(world, archetype);
    }

    public void ArchetypeAdded(Archetype archetype)
    {
        int index = archetype.GetComponentIndex(_componentID);
        if(index != 0)
        {
            MemoryHelpers.GetValueOrResize(ref _archetypes, _count++) = (archetype, archetype.Components[index]);
        }
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes)
    {
        var world = _world;
        foreach ((var archetype, _, int initalEntityCount) in archetypes)
        {
            int componentIndex = archetype.GetComponentIndex(_componentID);
            if(componentIndex != 0)
            {//this archetype has this component type
                archetype.Components[componentIndex].Run(world, archetype, initalEntityCount, archetype.EntityCount - initalEntityCount);
            }
        }
    }
}