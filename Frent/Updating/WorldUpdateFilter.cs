using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;

namespace Frent.Updating;

internal class WorldUpdateFilter : IComponentUpdateFilter
{
    private readonly World _world;
    //its entirely possible that the HashSet<Type> for this filter in GenerationServices.TypeAttributeCache doesn't even exist yet
    private readonly Type _attributeType;
    private int _lastRegisteredComponentID;
    
    private HashSet<Type>? _filter;
    
    //if we want, we can replace this with a byte[] array to save memory
    private ComponentStorageRecord[] _allComponents = new ComponentStorageRecord[8];
    private int _nextComponentStorageIndex;

    private readonly ShortSparseSet<(Archetype Archetype, int Start, int Length)> _archetypes = new();
    
    //these components need to be updated
    private FastStack<ComponentID> _filteredComponents = FastStack<ComponentID>.Create(8);
    
    public WorldUpdateFilter(World world, Type attributeType)
    {
        _attributeType = attributeType;
        _world = world;

        foreach (var archetype in world.EnabledArchetypes.AsSpan())
            ArchetypeAdded(archetype.Archetype(world)!);
    }

    public void Update()
    {
        World world = _world;
        Span<ComponentStorageRecord> componentStorages = _allComponents.AsSpan(0, _nextComponentStorageIndex);
        Span<(Archetype Archetype, int Start, int Length)> archetypes = _archetypes.AsSpan();
        for (int i = 0; i < archetypes.Length; i++)
        {
            (Archetype current, int start, int count) = archetypes[i];
            Span<ComponentStorageRecord> storages = componentStorages.Slice(start, count);
            foreach(var storage in storages)
            {
                storage.Run(world, current);
            }
        }
    }

    private void RegisterNewComponents()
    {
        if (_filter is null && 
            !GenerationServices.TypeAttributeCache.TryGetValue(_attributeType, out _filter))
            return;
        
        for (ref int i = ref _lastRegisteredComponentID; i < Component.ComponentTable.Count; i++)
        {
            ComponentID thisID = new((ushort)i);
            
            if(_filter.Contains(thisID.Type))
            {
                 _filteredComponents.Push(thisID);
            }
        }
    }

    internal void ArchetypeAdded(Archetype archetype)
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        int start = _nextComponentStorageIndex;
        int count = 0;
        foreach(var component in _filteredComponents.AsSpan())
        {
            int index = archetype.GetComponentIndex(component);
            if (index != 0) //archetype.Components[0] is always null; 0 tombstone value
            {
                count++;
                Debug.Assert(archetype.Components[index] is not null);
                if(_nextComponentStorageIndex == _allComponents.Length)
                    Array.Resize(ref _allComponents, _allComponents.Length * 2);
                _allComponents[_nextComponentStorageIndex++] = archetype.Components[index];
            }
        }

        if(count > 0)
            _archetypes[archetype.ID.RawIndex] = (archetype, start, count);
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes)
    {
        Span<ComponentStorageRecord> componentStorages = _allComponents.AsSpan(0, _nextComponentStorageIndex);
        foreach (var (archetype, _, count) in archetypes)
        {
            (Archetype current, int start, int end) = _archetypes[archetype.ID.RawIndex];

            foreach(var storage in componentStorages.Slice(start, end))
            {
                storage.Run(_world, current, count, current.EntityCount - count);
            }
        }
    }
}