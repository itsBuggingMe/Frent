﻿using Frent.Collections;
using Frent.Core;
using System.IO.Pipes;
using System.Runtime.CompilerServices;

namespace Frent.Updating.Threading;

internal static class FrentMultithread
{
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
        private Stack<ArchetypeUpdateRecord>? _archetypes;
        private ComponentStorageRecord[]? _componentStorageBases;
        private StrongBox<int>? _counter;

        public static void UnsafeQueueWork(World world, Stack<ArchetypeUpdateRecord> archetypes, ComponentStorageRecord[] componentStorageBases, StrongBox<int> counter)
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

                    Span<ComponentStorageRecord> storages = workItem._componentStorageBases.AsSpan(start, count);

                    foreach (var storage in storages)
                    {
                        //storage.Run(archetype, world);
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
        private ArchetypeUpdateRecord _archetypeRecord;
        private ComponentStorageRecord[]? _components;
        private StrongBox<int>? _counter;
        private int _start;
        private int _count;

        public static void UnsafeQueueWork(
            World world,
            ArchetypeUpdateRecord archetypeUpdateRecord,
            ComponentStorageRecord[] componentStorageBases,
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
                (Archetype current, int start, int count) = frentMultithreadWorkItem._archetypeRecord;
                Span<ComponentStorageRecord> storages = frentMultithreadWorkItem._components.AsSpan(start, count);

                int archetypeStart = frentMultithreadWorkItem._start;
                int archetypeCount = frentMultithreadWorkItem._count;

                foreach (var storage in storages)
                {
                    //storage.Run(current, world, archetypeStart, archetypeCount);
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