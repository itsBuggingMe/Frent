using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class EntityUpdateRunner<TComp> : IRunner
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
            entity.EntityVersion = world.EntityTable[entityId].Version;

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
            entity.EntityVersion = world.EntityTable[entityId].Version;

            int denseIndex = map[entityId];

            // ids in idsToUpdate are not guarenteed to be in this set
            if (denseIndex < 0)
            {
                continue;
            }

            Unsafe.Add(ref component, denseIndex).Update(entity);
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class EntityUpdateRunner<TComp, TArg> : IRunner
    where TComp : IEntityComponent<TArg>
{
    private static readonly Bitset SparseIncludeBits = new Bitset()
        .CompSet(Component<TArg>.SparseSetComponentIndex)
        ;

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
        Bitset includeBits = SparseIncludeBits;

        int end = length + start;
        for (int i = start; i < end; i++)
        {
            entityIds.SetEntity(ref entity);
            if (Component<TArg>.IsSparseComponent)
            {
                if ((uint)i < (uint)bitsets.Length)
                {
                    Bitset.AssertHasSparseComponents(ref bitsets[i], ref includeBits);
                }
                else
                {// has no sparse components, but we expected at least 1
                    FrentExceptions.Throw_NullReferenceException();
                }
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
                ? entity.GetCachedLookupAndAssertSparseComponent(world, SparseIncludeBits)
                : entity.GetCachedLookup(world);

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
                ? entity.GetCachedLookupAndAssertSparseComponent(world, SparseIncludeBits)
                : entity.GetCachedLookup(world);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            Unsafe.Add(ref component, denseIndex).Update(entity, ref arg);
        }
    }
}