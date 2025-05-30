using System;
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

    private readonly Type _attributeType;
    private int _lastRegisteredComponentID;
    
    private byte[] _componentRunnerIndices = new byte[8];

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
        Span<byte> componentStorages = _componentRunnerIndices.AsSpan(0, _nextComponentStorageIndex);
        Span<(Archetype Archetype, int Start, int Length)> archetypes = _archetypes.AsSpan();
        for (int i = 0; i < archetypes.Length; i++)
        {
            (Archetype current, int start, int count) = archetypes[i];
            Span<byte> storages = componentStorages.Slice(start, count);
            foreach(var index in storages)
            {
                var storageRecord = current.Components.UnsafeArrayIndex(index);
                storageRecord.Run(current, world);
            }
        }
    }

    private void RegisterNewComponents()
    {
        for (ref int i = ref _lastRegisteredComponentID; i < Component.ComponentTable.Count; i++)
        {
            ComponentID thisID = new((ushort)i);
            Type type = thisID.Type;

            if (GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out var arr) 
                && ContainsComponent(arr, type))
            {
                 _filteredComponents.Push(thisID);
            }
        }

        // optimize with 64 bit bloom filter?
        // 99% of cases there will not be more than 64 update types so bloom filter
        // will literally just become a bitset check
        static bool ContainsComponent(UpdateMethodData[] updateMethodData, Type type)
        {
            foreach (var methodData in updateMethodData)
            {
                if (Array.IndexOf(methodData.Attributes, type) != -1)
                    return true;
            }
            return false;
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
                // now, each component may have more than 1 runner

                Debug.Assert(archetype.Components[index].Buffer is not null);


            }
        }

        if(count > 0)
            _archetypes[archetype.ID.RawIndex] = (archetype, start, count);

        void PushIntoComponentRunner(byte index)
        {
            if (_nextComponentStorageIndex == _componentRunnerIndices.Length)
                Array.Resize(ref _componentRunnerIndices, _componentRunnerIndices.Length * 2);
            _componentRunnerIndices[_nextComponentStorageIndex++] = inde;
        }
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes)
    {
        Span<byte> componentStorages = _componentRunnerIndices.AsSpan(0, _nextComponentStorageIndex);
        foreach (var (archetype, _, count) in archetypes)
        {
            (Archetype current, int start, int end) = _archetypes[archetype.ID.RawIndex];

            foreach(var index in componentStorages.Slice(start, end))
            {
                var storage = current.Components.UnsafeArrayIndex(index);
                storage.Run(current, _world, count, current.EntityCount - count);
            }
        }
    }
}