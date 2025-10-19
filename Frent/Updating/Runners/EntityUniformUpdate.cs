using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class EntityUniformUpdateRunner<TPredicate, TComp, TUniform>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IEntityUniformComponent<TUniform>
{

    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref EntityIDOnly entityIds = ref Unsafe.Add(ref b.GetEntityDataReference(), start);
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        for (int i = 0; i < length; i++)
        {
            entityIds.SetEntity(ref entity);

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in b.GetBitsetNoLazy(i)))
                continue;

            comp.Update(entity, uniform);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world)
    {
        ref int entityId = ref sparseSet.GetEntityIDsDataReference();
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        for (int i = sparseSet.Count; i > 0; i--)
        {
            entity.EntityID = entityId;
            // I'm ok with pulling from the entity table
            // since the fact that they requested the entity implies that they are likely to use it too
            // not worth adding extra overhead to sparse sets
            ref var record = ref world.EntityTable[entityId];
            entity.EntityVersion = record.Version;

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(record.Archetype.ComponentTagTable), in record.Archetype.GetBitset(i)))
                continue;

            component.Update(entity, uniform);

            component = ref Unsafe.Add(ref component, 1);
            entityId = ref Unsafe.Add(ref entityId, 1);
        }
    }

    void IRunner.RunSparseSubset(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();
        ReadOnlySpan<int> map = sparseSet.SparseSpan();

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        foreach (var entityId in idsToUpdate)
        {
            if(!((uint)entityId < (uint)map.Length))
            {
                continue;
            }
            
            entity.EntityID = entityId;
            ref var record = ref world.EntityTable[entityId];
            entity.EntityVersion = record.Version;

            int denseIndex = map[entityId];

            // ids in idsToUpdate are not guarenteed to be in this set
            if (denseIndex < 0)
            {
                continue;
            }

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(record.Archetype.ComponentTagTable), in record.Archetype.GetBitset(record.Index)))
                continue;

            Unsafe.Add(ref component, denseIndex).Update(entity, uniform);
        }
    }
}


/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class EntityUniformUpdateRunner<TPredicate, TComp, TUniform, TArg>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IEntityUniformComponent<TUniform, TArg>
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

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        Span<Bitset> bitsets = b.SparseBitsetSpan();
        // TODO: double check that the jit register promotes this.
        // This needs to stay in a ymm register on x86
        Bitset includeBits = BitsetHelper<TArg>.BitsetOf;

        int end = length + start;
        for (int i = start; i < end; i++)
        {
            entityIds.SetEntity(ref entity);
            if (Component<TArg>.IsSparseComponent)
            {
                if ((uint)i < (uint)bitsets.Length)
                {
                    ref var bitset = ref bitsets[i];
                    Bitset.AssertHasSparseComponents(ref bitset, ref includeBits);

                    // we source our bitset from here so no double lookup
                    if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in bitset))
                        continue;
                }
                else
                {// has no sparse components, but we expected at least 1
                    FrentExceptions.Throw_NullReferenceException();
                }
            }
            // get bitset manually
            else if(typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in b.GetBitsetNoLazy(i)))
            {
                continue;
            }

            if (Component<TArg>.IsSparseComponent)
            {
                int index = sparseArgArray.UnsafeSpanIndex(entity.EntityID);
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(entity, uniform, ref arg);

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
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        for (int i = sparseSet.Count; i > 0; i--)
        {
            entity.EntityID = entityId;
            // entity version set in GetCachedLookup

            Archetype archetype;
            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, BitsetHelper<TArg>.BitsetOf, out archetype)
                : entity.GetCachedLookup(world, out archetype);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref entityData.MapRef, in archetype.GetBitset(i)))
                continue;

            component.Update(entity, uniform, ref arg);

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
        TUniform uniform = GetUniformOrValueTuple<TUniform>(world.UniformProvider);

        foreach (var entityId in idsToUpdate)
        {
            if (!((uint)entityId < (uint)map.Length))
                continue;
            int denseIndex = map[entityId];

            if (denseIndex < 0)
                continue;

            entity.EntityID = entityId;
            // entity version set in GetCachedLookup

            Archetype archetype;
            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, BitsetHelper<TArg>.BitsetOf, out archetype)
                : entity.GetCachedLookup(world, out archetype);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(archetype.ComponentTagTable), in archetype.GetBitset((int)entityData.Index)))
                continue;

            Unsafe.Add(ref component, denseIndex).Update(entity, uniform, ref arg);
        }
    }
}