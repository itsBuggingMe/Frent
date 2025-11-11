using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class EntityUpdateRunner<TPredicate, TComp>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IEntityComponent
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref EntityIDOnly entityIds = ref Unsafe.Add(ref b.GetEntityDataReference(), start);
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        Entity entity = world.DefaultWorldEntity;

        for (int i = length; i > 0; i--)
        {
            entityIds.SetEntity(ref entity);

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(b.ComponentTagTable), in b.GetBitsetNoLazy(i)))
                continue;

            comp.Update(entity);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world)
    {
        ref int entityId = ref sparseSet.GetEntityIDsDataReference();
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        Entity entity = world.DefaultWorldEntity;

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

            component.Update(entity);

            component = ref Unsafe.Add(ref component, 1);
            entityId = ref Unsafe.Add(ref entityId, 1);
        }
    }

    void IRunner.RunSparseSubset(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();
        ReadOnlySpan<int> map = sparseSet.SparseSpan();

        Entity entity = world.DefaultWorldEntity;

        foreach (var entityId in idsToUpdate)
        {
            if (!((uint)entityId < (uint)map.Length))
            {
                continue;
            }

            entity.EntityID = entityId;
            ref var record = ref world.EntityTable[entityId];
            entity.EntityVersion = world.EntityTable[entityId].Version;

            int denseIndex = map[entityId];

            // ids in idsToUpdate are not guarenteed to be in this set
            if (denseIndex < 0)
            {
                continue;
            }

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(record.Archetype.ComponentTagTable), in record.Archetype.GetBitset(record.Index)))
                continue;

            Unsafe.Add(ref component, denseIndex).Update(entity);
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class EntityUpdateRunner<TPredicate, TComp, TArg>(Delegate? f) : RunnerBase(f), IRunner
    where TPredicate : IFilterPredicate
    where TComp : IEntityComponent<TArg>
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
                int index = sparseArgArray.UnsafeSpanIndex(entity.EntityID);
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(entity, ref arg);

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

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref entityData.MapRef, in archetype.GetBitset(i)))
                continue;

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            component.Update(entity, ref arg);

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

            if (typeof(TPredicate) != typeof(NonePredicate) && default(TPredicate)!.SkipEntity(ref MemoryMarshal.GetArrayDataReference(archetype.ComponentTagTable), in archetype.GetBitset((int)entityData.Index)))
                continue;

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            Unsafe.Add(ref component, denseIndex).Update(entity, ref arg);
        }
    }
}