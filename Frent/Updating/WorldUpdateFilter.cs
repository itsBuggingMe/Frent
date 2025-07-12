using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Updating.Threading;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

internal class WorldUpdateFilter : IComponentUpdateFilter
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
    private ShortSparseSet<ArchetypeUpdateSpan> _matchedArchtypes = new();

    private ArchtypeUpdateMethod[] _methods = new ArchtypeUpdateMethod[8];
    private int _methodsCount;

    private Dictionary<ComponentID, FrugalRunnerArray> _matchedComponentMethods = [];
    private ulong _componentBloomFilter;

    private readonly StrongBox<int>? _updateCount;
    private readonly Stack<ArchetypeUpdateSpan>? _smallArchetypeUpdateRecords;
    private readonly Stack<ArchetypeUpdateSpan>? _largeArchetypeRecords;
    private readonly bool _isMultithread;

    public WorldUpdateFilter(World world, Type attributeType)
    {
        _isMultithread = typeof(MultithreadUpdateTypeAttribute).IsAssignableFrom(attributeType);
        _attributeType = attributeType;
        _world = world;

        foreach (var archetype in world.EnabledArchetypes.AsSpan())
            ArchetypeAdded(archetype.Archetype(world)!);

        if(_isMultithread)
        {
            _updateCount = new StrongBox<int>();
            _smallArchetypeUpdateRecords = new Stack<ArchetypeUpdateSpan>();
            _largeArchetypeRecords = new Stack<ArchetypeUpdateSpan>();
        }
    }

    public void Update()
    {
        if(_isMultithread)
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
        Span<ArchtypeUpdateMethod> records = _methods.AsSpan();
        World world = _world;
        foreach (var (archetype, start, length) in _matchedArchtypes.AsSpan())
        {
            ref ComponentStorageRecord archetypeFirst = ref MemoryMarshal.GetArrayDataReference(archetype.Components);
            foreach (ref var item in records.Slice(start, length))
            {
                Debug.Assert(item.Index < archetype.Components.Length);

                item.Runner.Run(Unsafe.Add(ref archetypeFirst, item.Index).Buffer, archetype, world);
            }
        }
    }

    private void MultithreadedUpdate()
    {
        const int LargeArchetypeThreshold = 16;

        Span<ArchtypeUpdateMethod> methods = _methods.AsSpan();
        var archetypes = _matchedArchtypes.AsSpan();

        int largeCount = 0;

        for (int i = 0; i < archetypes.Length; i++)
        {
            var record = archetypes[i];
            if (record.Archetype.EntityCount > LargeArchetypeThreshold)
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

            for (int j = 0; j < methods.Length; j++)
            {
                if (methods[j].AttributeIsDefined(_attributeType))
                {
                    matchedMethodsCount++;
                    matchedMethods |= 1UL << j;
                }
            }

            if(matchedMethodsCount > 0)
            {// something matched
                _componentBloomFilter |= 1UL << (i & 63);// set bloom filter bit
                FrugalRunnerArray frugalRunnerArray = default;
                IRunner[]? runners = null;

                if(matchedMethodsCount > 1)
                {
                    runners = new IRunner[matchedMethodsCount];
                    frugalRunnerArray = new FrugalRunnerArray(runners);
                }

                int k = 0;
                for (int j = 0; matchedMethods != default; j++, matchedMethods >>= 1)
                {
                    if ((matchedMethods & 1) == 0)
                    {
                        continue;
                    }

                    var runnerToSave = methods[j].Runner;

                    // index j has runner
                    if (matchedMethodsCount == 1)
                    {
                        frugalRunnerArray = new FrugalRunnerArray(runnerToSave);
                        break;
                    }
                    else
                    {
                        runners![k++] = runnerToSave;
                    }
                }

#if DEBUG
                // implicit null check
                _ = frugalRunnerArray.Length;
#endif
                _matchedComponentMethods.Add(thisID, frugalRunnerArray);
            }
        }
    }

    internal void ArchetypeAdded(Archetype archetype)
    {
        if (_lastRegisteredComponentID < Component.ComponentTable.Count)
            RegisterNewComponents();

        ImmutableArray<ComponentID> components = archetype.ID.Types;
        int start = _methodsCount;
        int length = 0;

        for(int i = 0; i < components.Length; i++)
        {
            ulong mask = 1UL << (components[i].RawIndex & 63);
            if ((mask & _componentBloomFilter) == 0 || !_matchedComponentMethods.TryGetValue(components[i], out var runners))
                continue;

            runners.GetOneOrOther(out IRunner[]? arr, out IRunner? single);

            if (single is not null)
            {
                PushArchetypeUpdateMethod(new ArchtypeUpdateMethod(single, i + 1 /*offset by one to account for tombstone at [0]*/));
            }
            else
            {
                foreach (var runner in arr!)
                {
                    PushArchetypeUpdateMethod(new ArchtypeUpdateMethod(runner, i + 1));
                }
            }
        }

        if(length != 0)
        {
            _matchedArchtypes[archetype.ID.RawIndex] = new(archetype, start, length);
        }

        void PushArchetypeUpdateMethod(ArchtypeUpdateMethod archtypeUpdateMethod)
        {
            MemoryHelpers.GetValueOrResize(ref _methods, _methodsCount++) = archtypeUpdateMethod;
            length++;
        }
    }

    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes)
    {
        Span<ArchtypeUpdateMethod> records = _methods.AsSpan();
        World world = _world;
        var archetypeSet = _matchedArchtypes;

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

                item.Runner.Run(Unsafe.Add(ref archetypeFirst, item.Index).Buffer, archetype, world, previousEntityCount, entitiesToUpdate);
            }
        }
    }

    /// <summary>
    /// An update method for a component type in the context of a specific archetype. The runner executes and the index is the index of the component buffer.
    /// </summary>
    internal readonly struct ArchtypeUpdateMethod(IRunner runner, nint index)
    {
        public readonly IRunner Runner = runner;
        public readonly nint Index = index;
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

    internal readonly struct FrugalRunnerArray
    {
        public FrugalRunnerArray(IRunner only)
        {
            _root = only;
        }

        public FrugalRunnerArray(IRunner[] runners)
        {
            _root = runners;
        }

        private readonly object _root;

        public readonly int Length
        {
            get
            {
                Debug.Assert(_root is not null);

                if(_root.GetType() == typeof(IRunner[]))
                {// n > 1
                    return UnsafeExtensions.UnsafeCast<IRunner[]>(_root).Length;
                }

                return 1;
            }
        }

        public void GetOneOrOther(out IRunner[]? runners, out IRunner? runner)
        {
            if (_root.GetType() == typeof(IRunner[]))
            {// n > 1
                runners = UnsafeExtensions.UnsafeCast<IRunner[]>(_root);
                runner = default;
                return;
            }
            runners = default;
            runner = UnsafeExtensions.UnsafeCast<IRunner>(_root); 
        }
    }
}