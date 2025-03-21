using System.Collections.Immutable;
using Frent.Collections;
using Frent.Core;

namespace Frent.Updating;

internal class WorldUpdateFilter
{
    private readonly World _world;
    private readonly ImmutableArray<ComponentID> _components;
    private int _lastRegisteredComponentID;

    //if we want, we can replace this with a byte[] array to save memory
    //however, i want to prioitze iteration speed since archetype fragmentation
    private FastStack<ComponentStorageBase> _allComponents = FastStack<ComponentStorageBase>.Create(8);
    private FastStack<(Archetype Archetype, nint Length)> _archetypes = FastStack<(Archetype Archetype, nint Length)>.Create(4);

    public WorldUpdateFilter(World world, ImmutableArray<ComponentID> components)
    {
        _components = components;
        _world = world;
    }

    public void Update(World world)
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        //for()
        {

        }
    }

    private void RegisterNewComponents()
    {

    }

    internal FastStack<ComponentID> Stack = FastStack<ComponentID>.Create(8);
    internal int NextComponentIndex;
}
