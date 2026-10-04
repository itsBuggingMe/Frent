using Frent.Collections;
using Frent.Core.Archetypes;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

internal partial class UpdateFilter
{
    internal class UpdateFilterParallelManager
#if !NETSTANDARD
        : IThreadPoolWorkItem
#endif
    {
        private FastStack<MultithreadWorkItem> _multithreadWorkItems = FastStack<MultithreadWorkItem>.Create(16);
        private readonly Stack<ArchetypeUpdateSpan> _smallArchetypeUpdateRecords = [];
        private readonly Stack<ArchetypeUpdateSpan> _largeArchetypeRecords = [];
        private readonly Stack<Exception> _multithreadExceptions = [];
        private readonly UpdateFilter _updateFilter;
#if NETSTANDARD
        private readonly WaitCallback _callbackInst;
#endif
        internal int _workIndex;
        internal int _activeWorkers;
        internal UpdateFilterParallelManager(UpdateFilter filter)
        {
            _updateFilter = filter;
#if NETSTANDARD
            _callbackInst = o => Execute();
#endif
    }

    internal void Update()
        {
            const int LargeArchetypeThreshold = 16;

            var archetypes = _updateFilter._matchedArchetypes.AsSpan();

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
                    _largeArchetypeRecords.Push(record);
                    largeCount += record.Archetype.EntityCount;
                }
                else
                {
                    _smallArchetypeUpdateRecords.Push(record);
                }
            }

            _multithreadWorkItems.Push(new MultithreadWorkItem(_smallArchetypeUpdateRecords));

            int maxChunkSize = Math.Max(largeCount / Environment.ProcessorCount, 256);

            while (_largeArchetypeRecords.TryPop(out var archetypeRecord))
            {
                int entityCount = archetypeRecord.Archetype.EntityCount;
                for (int i = 0; i < entityCount; i += maxChunkSize)
                {
                    _multithreadWorkItems.Push(new MultithreadWorkItem(archetypeRecord, i, Math.Min(maxChunkSize, entityCount - i)));
                }
            }

            Span<SparseUpdateMethod> sparseMethods = _updateFilter._sparseMethods.AsSpan(0, _updateFilter._sparseMethodsCount);

            for (int i = 0; i < sparseMethods.Length;)
            {
                ComponentSparseSetBase set = sparseMethods[i].SparseSet;
                if (set.Count == 0)
                {
                    i++;
                    continue;
                }

                int start = i;
                do
                {
                    i++;
                } while (i < sparseMethods.Length && set == sparseMethods[i].SparseSet);

                ArraySegment<SparseUpdateMethod> methods = new(_updateFilter._sparseMethods, start, i - start);
                _multithreadWorkItems.Push(new MultithreadWorkItem(methods));
            }

            try
            {
                Work();
            }
            finally
            {
                _multithreadWorkItems.Clear();
                if(_multithreadExceptions.Count > 0)
                {
                    Exception e = new AggregateException(_multithreadExceptions.ToArray());
                    _multithreadExceptions.Clear();
                    throw e;
                }
            }
        }

        internal void Work()
        {
            ThreadPool.GetMaxThreads(out int maxWorkers, out _);

            _workIndex = _multithreadWorkItems.Count;

            for (int i = 0; i < maxWorkers && i < _multithreadWorkItems.Count - 1; i++)
            {
#if !NETSTANDARD
                ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
                ThreadPool.UnsafeQueueUserWorkItem(_callbackInst, null);
#endif
            }

            while (Volatile.Read(ref _workIndex) >= 0)
            {
                Execute();
            }

            while (Volatile.Read(ref _activeWorkers) > 0)
            {
                // spin wait for active workers to finish
                // this should be fast
            }
        }

        public void Execute()
        {
            try
            {
                Interlocked.Increment(ref _activeWorkers);

                int workIndex = Interlocked.Decrement(ref _workIndex);
                if (workIndex < 0)
                    return;

                ExecuteWorkItem(ref _multithreadWorkItems[workIndex]);
            }
            catch(Exception ex)
            {
                lock(_multithreadExceptions)
                    _multithreadExceptions.Push(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _activeWorkers);
            }
        }

        internal void ExecuteWorkItem(ref MultithreadWorkItem workItem)
        {
            World world = _updateFilter._world;

            switch (workItem.Type)
            {
                case MultithreadWorkItemType.Chunk:
                    {
                        (Archetype archetype, int start, int count) = workItem.Chunk;
                        Span<ArchetypeUpdateMethod> methods = _updateFilter._methods.AsSpan(start, count);

                        int archetypeStart = workItem.ChunkStart;
                        int archetypeCount = workItem.ChunkLength;

                        ref ComponentStorageRecord storageStart = ref MemoryMarshal.GetArrayDataReference(archetype.Components);

                        foreach (var method in methods)
                        {
                            Debug.Assert(method.Index < archetype.Components.Length);

                            method.Runner.RunArchetypical(
                                Unsafe.Add(ref storageStart, method.Index).Buffer,
                                archetype,
                                world,
                                archetypeStart,
                                archetypeCount);
                        }
                    }
                    break;
                case MultithreadWorkItemType.ChunkCollection:
                    {
                        while (workItem.ChunkCollection!.TryPop(out var record))
                        {
                            (Archetype archetype, int start, int count) = record;

                            Span<ArchetypeUpdateMethod> methods = _updateFilter._methods.AsSpan(start, count);
                            ref ComponentStorageRecord storageStart = ref MemoryMarshal.GetArrayDataReference(archetype.Components);

                            foreach (var method in methods)
                            {
                                Debug.Assert(method.Index < archetype.Components.Length);

                                method.Runner.RunArchetypical(Unsafe.Add(ref storageStart, method.Index).Buffer, archetype, world, 0, archetype.EntityCount);
                            }
                        }
                    }
                    break;
                case MultithreadWorkItemType.SparseComponentCollection:
                    {
                        Span<SparseUpdateMethod> methods = workItem.SparseUpdateMethods;
                        ComponentSparseSetBase set = methods[0].SparseSet;

                        foreach (SparseUpdateMethod method in methods)
                        {
                            int entityId = 0;
                            method.Runner.RunSparse(set, world, ref entityId);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown work item type: {workItem.Type}");
            }
        }

    }

}