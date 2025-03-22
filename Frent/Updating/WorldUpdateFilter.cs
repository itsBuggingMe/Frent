using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;

namespace Frent.Updating;

internal class WorldUpdateFilter
{
    private readonly World _world;
    //its entirely possible that the HashSet<Type> for this filter in GenerationServices.TypeAttributeCache doesn't even exist yet
    private readonly Type _attributeType;
    private int _lastRegisteredComponentID;

    private HashSet<Type>? _filter;

    //if we want, we can replace this with a byte[] array to save memory
    //however, i want to prioitze iteration speed since archetype fragmentation
    private FastStack<ComponentStorageBase> _allComponents = FastStack<ComponentStorageBase>.Create(8);
    private FastStack<(Archetype Archetype, int Length)> _archetypes = FastStack<(Archetype Archetype, int Length)>.Create(4);
    private FastStack<ComponentID> _filteredComponents = FastStack<ComponentID>.Create(8);

    public WorldUpdateFilter(World world, Type attributeType)
    {
        _attributeType = attributeType;
        _world = world;
    }

    public void Update()
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        World world = _world;
        Span<ComponentStorageBase> componentStorages = _allComponents.AsSpan();
        Span<(Archetype Archetype, int Length)> archetypes = _archetypes.AsSpan();

        for (int i = 0; i < archetypes.Length; i++)
        {
            (Archetype current, int count) = archetypes[i];
            Span<ComponentStorageBase> storages = componentStorages[..count];
            componentStorages = componentStorages[count..];
            foreach(var item in storages)
            {
                item.Run(world, current);
            }
        }
    }

    private void RegisterNewComponents()
    {
        if (
            _filter is null && 
            !GenerationServices.TypeAttributeCache.TryGetValue(_attributeType, out _filter))
            return;

        for (ref int i = ref _lastRegisteredComponentID; i < Component.ComponentTable.Count; i++)
        {
            ComponentID thisID = new ComponentID((ushort)i);

            if(_filter.Contains(thisID.Type))
            {
                _filteredComponents.Push(thisID);
            }
        }
    }

    internal void WorldArchetypeAdded(Archetype archetype)
    {
        int count = 0;
        foreach(var component in _filteredComponents.AsSpan())
        {
            int index = archetype.GetComponentIndex(component);
            if (index != 0) //archetype.Components[0] is always null; 0 tombstone value
            {
                count++;
                _allComponents.Push(archetype.Components[index]);
            }
        }
        _archetypes.Push((archetype, count));
    }
}