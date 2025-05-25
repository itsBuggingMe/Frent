using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Updating.Threading;

namespace Frent.Updating;

internal class WorldUpdateFilter : IComponentUpdateFilter
{
    private readonly World _world;
    //its entirely possible that the HashSet<Type> for this filter in GenerationServices.TypeAttributeCache doesn't even exist yet
    private readonly Type _attributeType;
    private int _lastRegisteredComponentID;
    
    private HashSet<Type>? _filter;
    
    //if we want, we can replace this with a byte[] array to save memory
    private ComponentStorageBase[] _allComponents = new ComponentStorageBase[8];
    private int _nextComponentStorageIndex;

    private readonly ShortSparseSet<ArchetypeUpdateRecord> _archetypes = new();
    
    //these components need to be updated
    private FastStack<ComponentID> _filteredComponents = FastStack<ComponentID>.Create(8);

    private readonly bool _multithread;

    // The following fields are only used in multithreaded mode
    #region Multi
    private readonly StrongBox<int>? _updateCount;
    private readonly Stack<ArchetypeUpdateRecord>? _smallArchetypeUpdateRecords;
    private readonly Stack<ArchetypeUpdateRecord>? _largeArchetypeRecords;
    private int _largeArchetypeThreshold = 16; // this is dynamic
    #endregion

    public WorldUpdateFilter(World world, Type attributeType)
    {
        _multithread = typeof(MultithreadUpdateTypeAttribute).IsAssignableFrom(attributeType);
        if(_multithread)
        {
            _updateCount = new StrongBox<int>(0);
            _smallArchetypeUpdateRecords = [];
            _largeArchetypeRecords = [];
        }    
        _attributeType = attributeType;
        _world = world;

        foreach (var archetype in world.EnabledArchetypes.AsSpan())
            ArchetypeAdded(archetype.Archetype(world)!);
    }

    public void Update()
    {
        if (_multithread)
        {
            MultithreadedUpdate();
        }
        else
        {
            SinglethreadedUpdate();
        }
    }

    private void SinglethreadedUpdate()
    {
        World world = _world;
        Span<ComponentStorageBase> componentStorages = _allComponents.AsSpan(0, _nextComponentStorageIndex);
        Span<ArchetypeUpdateRecord> archetypes = _archetypes.AsSpan();
        for (int i = 0; i < archetypes.Length; i++)
        {
            (Archetype current, int start, int count) = archetypes[i];
            Span<ComponentStorageBase> storages = componentStorages.Slice(start, count);
            foreach (var storage in storages)
            {
                storage.Run(world, current);
            }
        }
    }

    private void MultithreadedUpdate()
    {
        Span<ArchetypeUpdateRecord> archetypes = _archetypes.AsSpan();

        int largeCount = 0;
        int smallCount = 0;

        for (int i = 0; i < archetypes.Length; i++)
        {
            var record = archetypes[i];
            if(record.Archetype.EntityCount > _largeArchetypeThreshold)
            {
                _largeArchetypeRecords!.Push(record);
                largeCount += record.Archetype.EntityCount;
            }
            else
            {
                _smallArchetypeUpdateRecords!.Push(record);
                smallCount += record.Archetype.EntityCount;
            }
        }

        FrentMultithread.MultipleArchetypeWorkItem.UnsafeQueueWork(
            _world, _smallArchetypeUpdateRecords!, _allComponents, _updateCount!);

        int maxChunkSize = largeCount / Environment.ProcessorCount;

        while (_largeArchetypeRecords!.TryPop(out var archetypeRecord))
        {
            int entityCount = archetypeRecord.Archetype.EntityCount;
            for (int i = 0; i < entityCount; i += maxChunkSize)
            {
                FrentMultithread.SingleArchetypeWorkItem.UnsafeQueueWork(
                    _world, archetypeRecord, _allComponents, _updateCount!, 
                    start: i, 
                    count: Math.Min(maxChunkSize, entityCount - i));
            }
        }

        // this thread has some time here to do busy work
        if(smallCount == 0)
        {
            
        }
        
        
        while (_updateCount!.Value > 0)
        {
            Thread.Yield();
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
            _archetypes[archetype.ID.RawIndex] = new(archetype, start, count);
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes)
    {
        Span<ComponentStorageBase> componentStorages = _allComponents.AsSpan(0, _nextComponentStorageIndex);
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