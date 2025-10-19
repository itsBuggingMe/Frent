﻿using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class UpdateRunner<TPredicate, TComp>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IComponent
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        for (int i = length; i > 0; i--)
        {
            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in b.GetBitsetNoLazy(i)))
                continue;

            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world)
    {
        ref int entityId = ref typeof(TPredicate) != typeof(NonePredicate) ?
            ref sparseSet.GetEntityIDsDataReference() :
            ref Unsafe.NullRef<int>();
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        for (int i = sparseSet.Count; i > 0; i--)
        {
            if (typeof(TPredicate) != typeof(NonePredicate))
            {
                Archetype archetype = world.EntityTable[entityId].Archetype;
                if (default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(archetype.ComponentTagTable), in archetype.GetBitset(i)))
                    continue;
            }

            component.Update();

            component = ref Unsafe.Add(ref component, 1);
            if (typeof(TPredicate) != typeof(NonePredicate))
                entityId = ref Unsafe.Add(ref entityId, 1);
        }
    }

    void IRunner.RunSparseSubset(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();
        ReadOnlySpan<int> map = sparseSet.SparseSpan();

        foreach (var entityId in idsToUpdate)
        {
            if (!((uint)entityId < (uint)map.Length))
            {
                continue;
            }

            int denseIndex = map[entityId];

            // ids in idsToUpdate are not guarenteed to be in this set
            if (denseIndex < 0)
            {
                continue;
            }


            if (typeof(TPredicate) != typeof(NonePredicate))
            {
                ref var record = ref world.EntityTable[entityId];
                Archetype archetype = record.Archetype;
                if (default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(archetype.ComponentTagTable), in archetype.GetBitset(record.Index)))
                    continue;
            }

            Unsafe.Add(ref component, denseIndex).Update();
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class UpdateRunner<TPredicate, TComp, TArg>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IComponent<TArg>
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref EntityIDOnly entityIds = ref Unsafe.Add(ref b.GetEntityDataReference(), start);
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);

        ref TArg sparseFirst = ref IRunner.InitSparse<TArg>(ref first, out Span<int> sparseArgArray);
        ref TArg arg = ref Component<TArg>.IsSparseComponent ?
            ref Unsafe.NullRef<TArg>()
            : ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);

        Span<Bitset> bitsets = b.SparseBitsetSpan();
        // TODO: double check that the jit register promotes this.
        // This needs to stay in a ymm register on x86
        Bitset includeBits = BitsetHelper<TArg>.BitsetOf;

        int end = length + start;
        for (int i = start; i < end; i++)
        {
            int entityId = entityIds.ID;

            if (Component<TArg>.IsSparseComponent)
            {
                if ((uint)i < (uint)bitsets.Length)
                {
                    ref var bitset = ref bitsets[i];
                    Bitset.AssertHasSparseComponents(ref bitset, ref includeBits);

                    if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in bitset))
                        continue;
                }
                else
                {// has no sparse components, but we expected at least 1
                    FrentExceptions.Throw_NullReferenceException();
                }
            }
            else if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in b.GetBitsetNoLazy(i)))
            {
                continue;
            }

            if (Component<TArg>.IsSparseComponent)
            {
                int index = sparseArgArray.UnsafeSpanIndex(entityId);
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(ref arg);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);

            if (!Component<TArg>.IsSparseComponent) arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world)
    {
        ref int entityId = ref sparseSet.GetEntityIDsDataReference();
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);

        // folded   
        ref TArg sparseFirst = ref IRunner.InitSparse<TArg>(ref first, out Span<int> sparseArgArray);

        Entity entity = world.DefaultWorldEntity;

        for (int i = sparseSet.Count; i > 0; i--)
        {
            entity.EntityID = entityId;
            // entity version set in GetCachedLookup

            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, BitsetHelper<TArg>.BitsetOf, out Archetype archetype)
                : entity.GetCachedLookup(world, out archetype);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref entityData.MapRef, in archetype.GetBitset(i)))
                continue;

            component.Update(ref arg);

            component = ref Unsafe.Add(ref component, 1);
            entityId = ref Unsafe.Add(ref entityId, 1);
        }
    }

    void IRunner.RunSparseSubset(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);
        ReadOnlySpan<int> map = sparseSet.SparseSpan();

        // folded   
        ref TArg sparseFirst = ref IRunner.InitSparse<TArg>(ref first, out Span<int> sparseArgArray);

        Entity entity = world.DefaultWorldEntity;

        foreach (var entityId in idsToUpdate)
        {
            if (!((uint)entityId < (uint)map.Length))
                continue;
            int denseIndex = map[entityId];

            if (denseIndex < 0)
                continue;

            entity.EntityID = entityId;
            // entity version set in GetCachedLookup

            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, BitsetHelper<TArg>.BitsetOf, out Archetype archetype)
                : entity.GetCachedLookup(world, out archetype);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(archetype.ComponentTagTable), in archetype.GetBitset((int)entityData.Index)))
                continue;

            Unsafe.Add(ref component, denseIndex).Update(ref arg);
        }
    }
}