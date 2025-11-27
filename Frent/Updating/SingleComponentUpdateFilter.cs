using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Updating.Runners;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Frent.Updating.AttributeUpdateFilter;

namespace Frent.Updating;

internal class SingleComponentUpdateFilter : IComponentUpdateFilter
{
    // archetypical
    private int _archetypesNext;
    private ArchetypeRecord[] _archetypes = [];
    private int _filteredRunnersPerArchetypeNext;
    private IRunner[] _filteredRunnersPerArchetype = [];

    // sparse
    public readonly ComponentSparseSetBase _sparseSet;

    // shared
    private readonly World _world;
    private readonly ComponentID _componentID;

    private readonly IRunner[] _normalRunners;
    private readonly (IDTypeFilter Filter, IRunner Runner)[] _filteredRunners;
    
    private readonly IRunner[] _allRunners;

    // bloom filter
    // used to check if a (Archetype, Runner) pair is in the set of matches
    // optimal k is (64 bits / items) * ln(2)
    // we will use k = 1
    private ulong _runnerArchetypeBloomBits;

    private readonly bool _isArchetypical;

    public SingleComponentUpdateFilter(World world, ComponentID component)
    {
        _world = world;
        _componentID = component;
        _isArchetypical = !component.IsSparseComponent;
        UpdateMethodData[] methods = component.Methods;
        IDTypeFilter[] filters = component.MethodFilters;
        _sparseSet = world.WorldSparseSetTable[component.SparseIndex];

        _allRunners = new IRunner[methods.Length];
        _normalRunners = new IRunner[methods.Length - filters.Length];
        _filteredRunners = filters.Length == 0 ? [] : new (IDTypeFilter Filter, IRunner Runner)[filters.Length];

        int nIndex = 0;
        int fIndex = 0;
        for (int i = 0; i < methods.Length; i++)
        {
            _allRunners[i] = methods[i].Runner;
            if ((uint)i < (uint)filters.Length && filters[i] is { } f && f != IDTypeFilter.None)
                _filteredRunners[fIndex++] = (f, methods[i].Runner);
            else
                _normalRunners[nIndex++] = methods[i].Runner;
        }

        foreach (var archetype in world.EnabledArchetypes)
        {
            ArchetypeAdded(archetype.Archetype(world));
        }
    }


    public void Update()
    {
        if (_isArchetypical)
        {
            int recordIndex = 0;

            try
            {
                if (_filteredRunners.Length == 0)
                {
                    SimpleArchetypicalUpdate();
                }
                else
                {
                    FilteredArchetypicalUpdate();
                }

                void SimpleArchetypicalUpdate()
                {
                    World world = _world;

                    Span<ArchetypeRecord> records = _archetypes.AsSpan(0, _archetypesNext);
                    for (int i = 0; i < records.Length; i++)
                    {
                        recordIndex = i;

                        (Archetype archetype, int storageIndex, int length) = records[i];

                        if (archetype.EntityCount == 0)
                            continue;

                        Array buffer = archetype.Components.UnsafeArrayIndex(storageIndex).Buffer;

                        foreach (var runner in _normalRunners)
                        {
                            runner.RunArchetypical(buffer, archetype, world, 0, archetype.EntityCount);
                        }
                    }
                }

                void FilteredArchetypicalUpdate()
                {
                    World world = _world;
                    ref IRunner current = ref MemoryMarshal.GetArrayDataReference(_filteredRunnersPerArchetype);

                    Span<ArchetypeRecord> records = _archetypes.AsSpan(0, _archetypesNext);
                    for (int i = 0; i < records.Length; i++)
                    {
                        recordIndex = i;

                        (Archetype archetype, int storageIndex, int length) = records[i];

                        int entityCount = archetype.EntityCount;

                        if (entityCount == 0)
                            continue;

                        Array buffer = archetype.Components.UnsafeArrayIndex(storageIndex).Buffer;

                        // average joe methods
                        foreach (var runner in _normalRunners)
                        {
                            runner.RunArchetypical(buffer, archetype, world, 0, entityCount);
                        }

                        for (nint j = 0; j < length; j++)
                        {
                            current.RunArchetypical(buffer, archetype, world, 0, entityCount);

                            current = ref Unsafe.Add(ref current, 1);
                        }
                    }
                }
            }
            catch(NullReferenceException)
            {
                if (CreateMissingComponentException(recordIndex) is { } m)
                    throw m;

                throw;
            }
        }
        else
        {
            int runnerIndex = 0;
            int entityId = 0;
            try
            {
                var set = _sparseSet;
                var world = _world;

                Span<IRunner> allRunners = _allRunners;
                for(; runnerIndex < allRunners.Length; runnerIndex++)
                {
                    allRunners[runnerIndex].RunSparse(set, world, ref entityId);
                }
            }
            catch(NullReferenceException)
            {
                IRunner failedRunner = _allRunners[runnerIndex];
                MissingComponentException? e = FrentExceptions.CreateExceptionSparse(_world, _sparseSet, entityId, m => m.Runner == failedRunner);

                if (e is not null)
                    throw e;

                throw;
            }
        }
    }

    private MissingComponentException? CreateMissingComponentException(int recordIndex)
    {
        Span<ArchetypeRecord> records = _archetypes.AsSpan(0, _archetypesNext);

        int start = 0;
        for (int i = 0; i < recordIndex; i++)
            start += records[i].Length;

        Span<IRunner> filteredRunners = _filteredRunnersPerArchetype.AsSpan(start, records[recordIndex].Length);
        Span<IRunner> normalRunners = _normalRunners;

        (Archetype archetype, int storageIndex, _) = records[recordIndex];
        UpdateMethodData[] metadata = _componentID.Methods;

        return CheckRunners(filteredRunners) ?? CheckRunners(normalRunners);


        MissingComponentException? CheckRunners(Span<IRunner> runners)
        {
            foreach (var runner in runners)
            {
                int index = MemoryHelpers.FindIndexOfRunner(metadata, runner);
                if (index is -1)
                    continue;

                Type[] deps = metadata[index].Dependencies;

                foreach (EntityIDOnly entityId in archetype.GetEntitySpan())
                {
                    Entity e = entityId.ToEntity(_world);

                    foreach (var dep in deps)
                    {
                        if (!e.Has(dep))
                            return new MissingComponentException(_componentID.Type, dep, e);
                    }
                }
            }

            return null;
        }
    }

    public void ArchetypeAdded(Archetype archetype)
    {
        if (!_isArchetypical)
            return;

        int componentIndex = archetype.GetComponentIndex(_componentID);
        if (componentIndex is 0)
            return;

        int length = 0;

        foreach ((IDTypeFilter filter, IRunner runner) in _filteredRunners)
        {
            if (filter.FilterArchetype(archetype))
            {
                MemoryHelpers.GetValueOrResize(ref _filteredRunnersPerArchetype, _filteredRunnersPerArchetypeNext++) = runner;
                length++;

                // k == 1
                _runnerArchetypeBloomBits |= BloomBit(runner, archetype);
            }
        }

        foreach (IRunner runner in _normalRunners)
        {
            MemoryHelpers.GetValueOrResize(ref _filteredRunnersPerArchetype, _filteredRunnersPerArchetypeNext++) = runner;
            length++;
        }

        if (length != 0)
            MemoryHelpers.GetValueOrResize(ref _archetypes, _archetypesNext++)
                = new ArchetypeRecord(archetype, componentIndex, length);
    }

    private ulong BloomBit(IRunner runner, Archetype archetype)
    {
        int hashCode = 31 * (527 + runner.GetHashCode()) + archetype.ID.RawIndex;
        return (1UL << (hashCode & 63));
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes, ReadOnlySpan<int> ids)
    {
        ComponentID componentID = _componentID;
        World world = _world;

        if (_isArchetypical)
        {
            foreach ((Archetype archetype, Archetype _, int initalEntityCount) in archetypes)
            {
                Array buffer = archetype.Components.UnsafeArrayIndex(archetype.GetComponentIndex(componentID)).Buffer;
                int entityCount = archetype.EntityCount;

                // average joe methods
                foreach (var runner in _normalRunners)
                {
                    runner.RunArchetypical(buffer, archetype, world, 0, entityCount);
                }

                foreach ((IDTypeFilter filter, IRunner runner) in _filteredRunners)
                {
                    if ((_runnerArchetypeBloomBits & BloomBit(runner, archetype)) == 0)
                        continue;
                    if (!filter.FilterArchetype(archetype))
                        continue;

                    runner.RunArchetypical(buffer, archetype, world, 0, entityCount);
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    record struct ArchetypeRecord(Archetype Archetype, int StorageIndex, int Length /*number of runners in the _filteredRunnersPerArchetype to update*/);
}