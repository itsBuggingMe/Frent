using Frent.Collections;
using Frent.Core;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using static Frent.Updating.AttributeUpdateFilter;

namespace Frent.Updating.Threading;

internal static class FrentMultithread
{
    internal class SparseSetWorkItem
    {
        private static readonly
#if NET9_0_OR_GREATER
            Lock s_poolLock = new();
#else
        object s_poolLock = new();
#endif

        private static FastStack<SparseSetWorkItem> s_pool = FastStack<SparseSetWorkItem>.Create(1);

        private World? _world;
        private ArraySegment<SparseUpdateMethod> _update;
        private StrongBox<int>? _counter;

        public static void UnsafeQueueWork(World world, ArraySegment<SparseUpdateMethod> method, StrongBox<int> counter)
        {
            if(method.Count == 0)
                return;

            Interlocked.Increment(ref counter.Value);

            SparseSetWorkItem workItem;

            lock (s_poolLock)
            {
                workItem = s_pool.TryPop(out var w) ? w : new();
            }

            workItem._world = world;
            workItem._update = method;
            workItem._counter = counter;

            ThreadPool.UnsafeQueueUserWorkItem(static o =>
            {
                SparseSetWorkItem workItem = UnsafeExtensions.UnsafeCast<SparseSetWorkItem>(o!);

                Span<SparseUpdateMethod> methods = workItem._update.AsSpan();
                ComponentSparseSetBase set = methods[0].SparseSet;

                foreach (SparseUpdateMethod method in methods)
                {
                    method.Runner.RunSparse(set, workItem._world!);
                }

                Interlocked.Decrement(ref workItem._counter!.Value);

                workItem._world = default;
                workItem._update = default;
                workItem._counter = default;

                lock (s_poolLock)
                {
                    s_pool.Push(workItem);
                }
            }, workItem);
        }
    }

    internal class MultipleArchetypeWorkItem
    {
        private static readonly
#if NET9_0_OR_GREATER
        Lock s_poolLock = new();
#else
        object s_poolLock = new();
#endif

        private static FastStack<MultipleArchetypeWorkItem> s_pool = FastStack<MultipleArchetypeWorkItem>.Create(1);

        private World? _world;
        private Stack<ArchetypeUpdateSpan>? _archetypes;
        private ArchtypeUpdateMethod[]? _componentStorageBases;
        private StrongBox<int>? _counter;

        public static void UnsafeQueueWork(World world, Stack<ArchetypeUpdateSpan> archetypes, ArchtypeUpdateMethod[] componentStorageBases, StrongBox<int> counter)
        {
            Interlocked.Increment(ref counter.Value);

            MultipleArchetypeWorkItem workItem;

            lock (s_poolLock)
            {
                workItem = s_pool.TryPop(out var w) ? w : new();
            }

            workItem._archetypes = archetypes;
            workItem._componentStorageBases = componentStorageBases;
            workItem._world = world;
            workItem._counter = counter;

            ThreadPool.UnsafeQueueUserWorkItem(static o =>
            {
                MultipleArchetypeWorkItem workItem = UnsafeExtensions.UnsafeCast<MultipleArchetypeWorkItem>(o!);

                World world = workItem._world!;

                while(workItem._archetypes!.TryPop(out var record))
                {
                    (Archetype archetype, int start, int count) = record;

                    Span<ArchtypeUpdateMethod> methods = workItem._componentStorageBases.AsSpan(start, count);
                    ref ComponentStorageRecord storageStart = ref MemoryMarshal.GetArrayDataReference(archetype.Components);

                    foreach (var method in methods)
                    {
                        Debug.Assert(method.Index < archetype.Components.Length);

                        method.Runner.RunArchetypical(Unsafe.Add(ref storageStart, method.Index).Buffer, archetype, world, 0, archetype.EntityCount);
                    }
                }

                Interlocked.Decrement(ref workItem._counter!.Value);

                workItem._archetypes = default;
                workItem._componentStorageBases = default;
                workItem._world = default;
                workItem._counter = default;

                lock (s_poolLock)
                {
                    s_pool.Push(workItem);
                }
            }, workItem);
        }
    }

    internal class SingleArchetypeWorkItem
    {
        private static readonly
#if NET9_0_OR_GREATER
        Lock s_poolLock = new();
#else
        object s_poolLock = new();
#endif

        private static FastStack<SingleArchetypeWorkItem> s_pool = FastStack<SingleArchetypeWorkItem>.Create(1);

        private World? _world;
        private ArchetypeUpdateSpan _archetypeRecord;
        private ArchtypeUpdateMethod[]? _components;
        private StrongBox<int>? _counter;
        private int _start;
        private int _count;

        public static void UnsafeQueueWork(
            World world,
            ArchetypeUpdateSpan archetypeUpdateRecord,
            ArchtypeUpdateMethod[] componentStorageBases,
            StrongBox<int> counter,
            int start,
            int count)
        {
            Interlocked.Increment(ref counter.Value);
             
            SingleArchetypeWorkItem workItem;

            lock (s_poolLock)
            {
                workItem = s_pool.TryPop(out var w) ? w : new();
            }

            workItem._archetypeRecord = archetypeUpdateRecord;
            workItem._components = componentStorageBases;
            workItem._world = world;
            workItem._counter = counter;
            workItem._start = start;
            workItem._count = count;

            ThreadPool.UnsafeQueueUserWorkItem(static o =>
            {
                SingleArchetypeWorkItem frentMultithreadWorkItem = UnsafeExtensions.UnsafeCast<SingleArchetypeWorkItem>(o!);

                World world = frentMultithreadWorkItem._world!;
                (Archetype archetype, int start, int count) = frentMultithreadWorkItem._archetypeRecord;
                Span<ArchtypeUpdateMethod> methods = frentMultithreadWorkItem._components.AsSpan(start, count);

                int archetypeStart = frentMultithreadWorkItem._start;
                int archetypeCount = frentMultithreadWorkItem._count;

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

                Interlocked.Decrement(ref frentMultithreadWorkItem._counter!.Value);

                frentMultithreadWorkItem._archetypeRecord = default;
                frentMultithreadWorkItem._components = default;
                frentMultithreadWorkItem._world = default;
                frentMultithreadWorkItem._counter = default;

                lock (s_poolLock)
                {
                    s_pool.Push(frentMultithreadWorkItem);
                }
            }, workItem);
        }
    }


}