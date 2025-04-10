using Frent.Core;

namespace Frent.Updating;

internal class SingleComponentUpdateFilter
{
    private (Archetype Archetype, ComponentStorageBase Storage)[] _archetypes = [];
    private int _count;
    private ComponentID _componentID;

    public SingleComponentUpdateFilter(ComponentID component)
    {
        _componentID = component;
    }


    public void Update(World world)
    {
        foreach(var (archetype, storage) in _archetypes)
            storage.Run(world,archetype);
    }

    public void ArchetypeAdded(Archetype archetype)
    {
        int index = archetype.GetComponentIndex(_componentID);
        if(index != 0)
        {
            MemoryHelpers.GetValueOrResize(ref _archetypes, _count++) = (archetype, archetype.Components[index]);
        }
    }
}