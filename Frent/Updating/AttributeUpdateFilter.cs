using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Updating.Runners;
using Frent.Updating.Threading;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

internal class AttributeUpdateFilter : IComponentUpdateFilter
{
    /*  In each world, there are n archetype that match the filter, where n >= 0.
     *  In each archetype there are m component types that match the filter, where m >= 0.
     *  In each component type in the archetype, o methods that match the filter, where o > 0.
     *  In each method there are p attributes, one of which is the attribute type we are filtering by.
     *  My head hurts.
     */

    private readonly World _world;
    private readonly Type _attributeType;


    private int _lastRegisteredComponentID;
    private readonly ShortSparseSet<ArchetypeUpdateSpan> _matchedArchetypes = new();

    private ArchetypeUpdateMethod[] _methods = new ArchetypeUpdateMethod[8];
    private int _methodsCount;

    private SparseUpdateMethod[] _sparseMethods = new SparseUpdateMethod[4];
    private int _sparseMethodsCount;

    private readonly Dictionary<ComponentID, MatchedMethodData> _matchedArchetypicalComponentMethods = [];
    private ulong _componentBloomFilter;

    private readonly StrongBox<int>? _updateCount;
    private readonly Stack<ArchetypeUpdateSpan>? _smallArchetypeUpdateRecords;
    private readonly Stack<ArchetypeUpdateSpan>? _largeArchetypeRecords;
    private readonly bool _isMultithread;
    private readonly bool _matchAll;

    public AttributeUpdateFilter(World world, Type attributeType, bool overrideMatchAll)
    {
        _isMultithread = typeof(MultithreadUpdateTypeAttribute).IsAssignableFrom(attributeType);
        _attributeType = attributeType;
        _world = world;
        _matchAll = overrideMatchAll;

        foreach (var archetype in world.EnabledArchetypes.AsSpan())
            ArchetypeAdded(archetype.Archetype(world)!);

        if (_isMultithread)
        {
            _updateCount = new StrongBox<int>();
            _smallArchetypeUpdateRecords = new Stack<ArchetypeUpdateSpan>();
            _largeArchetypeRecords = new Stack<ArchetypeUpdateSpan>();
        }
    }

    public void Update()
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        if (_isMultithread)
        {
            MultiThreadedUpdate();
        }
        else
        {
            int recordIndex = 0;

            try
            {
                SingleThreadedArchetype(ref recordIndex);
            }
            catch (NullReferenceException) // translate null reference exceptions into missing component exceptions
            {
                if (CreateExceptionArchetype(recordIndex) is { } e)
                    throw e;
                throw;
            }

            int entityId = 0;
            int sparseRecordIndex = 0;

            try
            {
                SingleThreadedSparse(ref sparseRecordIndex, ref entityId);
            }
            catch (NullReferenceException)
            {
                if (CreateExceptionSparse(entityId, sparseRecordIndex) is { } e)
                    throw e;
                throw;
            }
        }
    }

    private void SingleThreadedArchetype(ref int archetypeRecordIndex)
    {
        Span<ArchetypeUpdateMethod> records = _methods.AsSpan();
        World world = _world;

        Span<ArchetypeUpdateSpan> archetypes = _matchedArchetypes.AsSpan();
        for(archetypeRecordIndex = 0; archetypeRecordIndex < archetypes.Length; archetypeRecordIndex++)
        {
            ref ArchetypeUpdateSpan archetypeRecord = ref archetypes.UnsafeSpanIndex(archetypeRecordIndex);

            Archetype archetype = archetypeRecord.Archetype;
            int start = archetypeRecord.Start;
            int length = archetypeRecord.Length;

            if (archetype.EntityCount == 0)
                continue;

            ref ComponentStorageRecord archetypeFirst = ref MemoryMarshal.GetArrayDataReference(archetype.Components);
            foreach (ref var item in records.Slice(start, length))
            {
                Debug.Assert(item.Index < archetype.Components.Length);

                item.Runner.RunArchetypical(Unsafe.Add(ref archetypeFirst, item.Index).Buffer, archetype, world, 0, archetype.EntityCount);
            }
        }
    }

    private void SingleThreadedSparse(ref int i, ref int sparseEntityId)
    {
        World world = _world;
        Span<SparseUpdateMethod> sparseUpdates = _sparseMethods.AsSpan(0, _sparseMethodsCount);

        for(i = 0; i < sparseUpdates.Length; i++)
        {
            SparseUpdateMethod method = sparseUpdates[i];

            if (method.SparseSet.Count == 0)
                continue;

            method.Runner.RunSparse(method.SparseSet, world, ref sparseEntityId);
        }
    }

    private void MultiThreadedUpdate()
    {
        const int LargeArchetypeThreshold = 16;

        var archetypes = _matchedArchetypes.AsSpan();

        int largeCount = 0;

        for (int i = 0; i < archetypes.Length; i++)
        {
            var record = archetypes[i];
            if (record.Archetype.EntityCount == 0)
            {
                continue;
            }
            else if (record.Archetype.EntityCount > LargeArchetypeThreshold)
            {
                _largeArchetypeRecords!.Push(record);
                largeCount += record.Archetype.EntityCount;
            }
            else
            {
                _smallArchetypeUpdateRecords!.Push(record);
            }
        }

        FrentMultithread.MultipleArchetypeWorkItem.UnsafeQueueWork(
            _world, _smallArchetypeUpdateRecords!, _methods, _updateCount!);

        int maxChunkSize = Math.Max(largeCount / Environment.ProcessorCount, 256);

        while (_largeArchetypeRecords!.TryPop(out var archetypeRecord))
        {
            int entityCount = archetypeRecord.Archetype.EntityCount;
            for (int i = 0; i < entityCount; i += maxChunkSize)
            {
                FrentMultithread.SingleArchetypeWorkItem.UnsafeQueueWork(
                    _world, archetypeRecord, _methods, _updateCount!,
                    start: i,
                    count: Math.Min(maxChunkSize, entityCount - i));
            }
        }

        Span<SparseUpdateMethod> sparseMethods = _sparseMethods.AsSpan(0, _sparseMethodsCount);

        for (int i = 0; i < sparseMethods.Length; i++)
        {
            ComponentSparseSetBase set = sparseMethods[i].SparseSet;
            if (set.Count == 0)
                continue;

            int start = i;
            do
            {
                i++;
            } while (i < sparseMethods.Length && set == sparseMethods[i].SparseSet);

            ArraySegment<SparseUpdateMethod> methods = new(_sparseMethods, start, i - start);
            FrentMultithread.SparseSetWorkItem.UnsafeQueueWork(_world, methods, _updateCount!);
        }
    }

    private void RegisterNewComponents()
    {
        for (ref int i = ref _lastRegisteredComponentID; i < Component.ComponentTable.Count; i++)
        {
            ComponentID thisID = new((ushort)i);
            Type type = thisID.Type;

            ulong matchedMethods = default;
            int matchedMethodsCount = 0;
            UpdateMethodData[] methods = thisID.Methods;
            IDTypeFilter[] typeFilters = thisID.MethodFilters;

            for (int j = 0; j < methods.Length; j++)
            {
                if (_matchAll || methods[j].AttributeIsDefined(_attributeType))
                {
                    matchedMethodsCount++;
                    matchedMethods |= 1UL << j;
                }
            }

            if (matchedMethodsCount > 0)
            {// something matched
                if (thisID.IsSparseComponent)
                {
                    for (int j = 0; j < methods.Length; j++)
                    {
                        if(((1UL << j) & matchedMethods) != 0)
                            MemoryHelpers.GetValueOrResize(ref _sparseMethods, _sparseMethodsCount++) = 
                                new SparseUpdateMethod(methods[j].Runner, _world.WorldSparseSetTable[thisID.SparseIndex]);
                    }
                    continue;
                }

                // for archetypical ones, we need to wait for an archetype to show up to actually do something
                // store which update methods match for this component

                _componentBloomFilter |= 1UL << (i & 63);// set bloom filter bit
                MatchedMethodData frugalRunnerArray = default;
                IRunner[]? runners = null;
                IDTypeFilter[]? filters = null;
                int[]? indicies = null;

                if (matchedMethodsCount > 1)
                {
                    runners = new IRunner[matchedMethodsCount];
                    filters = new IDTypeFilter[matchedMethodsCount];
                    indicies = new int[matchedMethodsCount];
                    frugalRunnerArray = new MatchedMethodData(runners, filters, indicies);
                }

                int k = 0;
                for (int j = 0; matchedMethods != default; j++, matchedMethods >>= 1)
                {
                    if ((matchedMethods & 1) == 0)
                    {
                        continue;
                    }

                    var runnerToSave = methods[j].Runner;
                    var filterToSave = j < typeFilters.Length ? typeFilters[j] : IDTypeFilter.None;

                    // index j has runner
                    if (matchedMethodsCount == 1)
                    {
                        frugalRunnerArray = new MatchedMethodData(runnerToSave, filterToSave, j);
                        break;
                    }
                    else
                    {
                        runners![k] = runnerToSave;
                        filters![k] = filterToSave;
                        indicies![k++] = j;
                    }
                }

#if DEBUG
                // implicit null check
                _ = frugalRunnerArray.Length;
#endif

                _matchedArchetypicalComponentMethods.Add(thisID, frugalRunnerArray);
            }
        }
    }

    private MissingComponentException? CreateExceptionArchetype(int matchedArchetypeIndex)
    {
        ArchetypeUpdateSpan record = _matchedArchetypes.AsSpan()[matchedArchetypeIndex];
        Archetype archetype = record.Archetype;

        if(archetype.EntityCount == 0)
            return null;

        foreach(ref ArchetypeUpdateMethod potentialFailure in _methods.AsSpan(record.Start, record.Length))
        {
            // ComponentID -> potentialFailure.Index
            byte[] tagTable = record.Archetype.ComponentTagTable;
            
            for(int i = 0; i < tagTable.Length; i++)
                if ((tagTable[i] & GlobalWorldTables.IndexBits) == potentialFailure.Index)
                {
                    ComponentID failedComponent = new((ushort)i);

                    // loop through depdendencies of this component to see if any are missing
                    UpdateMethodData metadata = failedComponent.Methods[potentialFailure.MetadataIndex];
                    foreach(var dependency in metadata.Dependencies)
                    {
                        if(archetype.GetComponentIndex(Component.GetComponentID(dependency)) == 0)
                        {
                            Entity firstEntity = archetype.GetEntityDataReference().ToEntity(_world);
                            return new MissingComponentException(failedComponent.Type, dependency, firstEntity);
                        }
                    }
                }
        }

        // everything in order, must be user null reference exception
        return null;
    }

    private MissingComponentException? CreateExceptionSparse(int entityId, int recordId)
    {
        SparseUpdateMethod record = _sparseMethods[recordId];

        ComponentID componentId = Component.GetComponentID(record.SparseSet.Type);

        Entity e = new Entity(_world.WorldID, _world.EntityTable[entityId].Version, entityId);

        Debug.Assert(_world.EntityTable[entityId].Archetype is not null);

        foreach (var method in componentId.Methods)
        {
            if(_matchAll || method.AttributeIsDefined(_attributeType))
            {
                foreach(var dep in method.Dependencies)
                {
                    if(!e.Has(dep))
                        return new MissingComponentException(componentId.Type, dep, e);
                }
            }
        }

        return null;
    }
    internal void ArchetypeAdded(Archetype archetype)
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        ImmutableArray<ComponentID> components = archetype.ID.Types;
        int start = _methodsCount;
        int length = 0;

        for (int i = 0; i < components.Length; i++)
        {
            ulong mask = 1UL << (components[i].RawIndex & 63);
            if ((mask & _componentBloomFilter) == 0 || !_matchedArchetypicalComponentMethods.TryGetValue(components[i], out var runners))
                continue;

            runners.GetOneOrOther(
                out IRunner[]? arr, out IRunner? single,
                out IDTypeFilter[]? arrF, out IDTypeFilter? singleF,
                out int[]? arrI, out int? singleI
                );



            if (single is not null)
            {
                if (singleF!.FilterArchetype(archetype))
                    PushArchetypeUpdateMethod(new ArchetypeUpdateMethod(single, i + 1 /*offset by one to account for tombstone at [0]*/, singleI!.Value));
            }
            else
            {
                for (int j = 0; j < arr!.Length; j++)
                {
                    if (arrF![j].FilterArchetype(archetype))
                        PushArchetypeUpdateMethod(new ArchetypeUpdateMethod(arr[j], i + 1, arrI![j]));
                }
            }
        }

        if (length != 0)
        {
            _matchedArchetypes[archetype.ID.RawIndex] = new(archetype, start, length);
        }

        void PushArchetypeUpdateMethod(ArchetypeUpdateMethod archtypeUpdateMethod)
        {
            MemoryHelpers.GetValueOrResize(ref _methods, _methodsCount++) = archtypeUpdateMethod;
            length++;
        }
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes, ReadOnlySpan<int> ids)
    {
        Span<ArchetypeUpdateMethod> records = _methods.AsSpan();
        World world = _world;
        var archetypeSet = _matchedArchetypes;

        foreach (var (archetype, _, previousEntityCount) in archetypes)
        {
            ref ComponentStorageRecord archetypeFirst = ref MemoryMarshal.GetArrayDataReference(archetype.Components);

            if (!archetypeSet.TryGet(archetype.ID.RawIndex, out var archetypeSpan))
                continue;

            var (_, start, length) = archetypeSpan;
            int entitiesToUpdate = archetype.EntityCount - previousEntityCount;

            foreach (ref var item in records.Slice(start, length))
            {
                Debug.Assert(item.Index < archetype.Components.Length);

                item.Runner.RunArchetypical(Unsafe.Add(ref archetypeFirst, item.Index).Buffer, archetype, world, previousEntityCount, entitiesToUpdate);
            }
        }

        foreach (var sparseMethod in _sparseMethods.AsSpan(0, _sparseMethodsCount))
        {
            int entityId = 0;
            sparseMethod.Runner.RunSparseSubset(sparseMethod.SparseSet, world, ids, ref entityId);
        }
    }

    /// <summary>
    /// An update method for a component type in the context of a specific archetype. The runner executes and the index is the index of the component buffer.
    /// </summary>
    internal readonly struct ArchetypeUpdateMethod(IRunner runner, int index, int metadataIndex)
    {
        public readonly IRunner Runner = runner;
        public readonly int Index = index;

        // UpdateMethodData[MetadataIndex].Runner == Runner
        // Maps backwards to metadata for exception handling
        public readonly int MetadataIndex = metadataIndex;
    }

    internal readonly struct SparseUpdateMethod(IRunner runner, ComponentSparseSetBase sparseSet)
    {
        public readonly IRunner Runner = runner;
        public readonly ComponentSparseSetBase SparseSet = sparseSet;
    }

    internal readonly struct ArchetypeUpdateSpan(Archetype archetype, int start, int length)
    {
        public readonly Archetype Archetype = archetype;
        public readonly int Start = start;
        public readonly int Length = length;

        public void Deconstruct(out Archetype archetype, out int start, out int length)
        {
            archetype = Archetype;
            start = Start;
            length = Length;
        }
    }

    internal readonly struct MatchedMethodData
    {
        public MatchedMethodData(IRunner only, IDTypeFilter typeFilterRecord, int metadataIndex)
        {
            _root = only;
            _filterRoot = typeFilterRecord;
            _metadataIndexRoot = metadataIndex;
        }

        public MatchedMethodData(IRunner[] runners, IDTypeFilter[] typeFilterRecord, int[] metadataIndices)
        {
            _root = runners;
            _filterRoot = typeFilterRecord;
            _metadataIndexRoot = metadataIndices;
        }

        private readonly object _root;
        private readonly object _filterRoot;
        private readonly object _metadataIndexRoot;

        public readonly int Length
        {
            get
            {
                Debug.Assert(_root is not null);

                if (_root.GetType() == typeof(IRunner[]))
                {// n > 1
                    return UnsafeExtensions.UnsafeCast<IRunner[]>(_root).Length;
                }

                return 1;
            }
        }

        public void GetOneOrOther(
            out IRunner[]? runners, out IRunner? runner,
            out IDTypeFilter[]? typeFilterRecords, out IDTypeFilter? typeFilterRecord,
            out int[]? metadataIndicies, out int? metadataIndex
            )
        {
            if (_root.GetType() == typeof(IRunner[]))
            {// n > 1
                runners = UnsafeExtensions.UnsafeCast<IRunner[]>(_root);
                runner = default;
                typeFilterRecords = UnsafeExtensions.UnsafeCast<IDTypeFilter[]>(_filterRoot);
                typeFilterRecord = default;
                metadataIndicies = UnsafeExtensions.UnsafeCast<int[]>(_metadataIndexRoot);
                metadataIndex = default;
                return;
            }
            runners = default;
            runner = UnsafeExtensions.UnsafeCast<IRunner>(_root);
            typeFilterRecords = default;
            typeFilterRecord = UnsafeExtensions.UnsafeCast<IDTypeFilter>(_filterRoot);
            metadataIndicies = default;
            metadataIndex = (int)_metadataIndexRoot;
        }
    }
}