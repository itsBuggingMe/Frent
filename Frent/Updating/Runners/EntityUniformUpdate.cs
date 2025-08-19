using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class EntityUniformUpdateRunner<TComp, TUniform> : IRunner
    where TComp : IEntityUniformComponent<TUniform>
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref EntityIDOnly entityIds = ref Unsafe.Add(ref b.GetEntityDataReference(), start);
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);
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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = sparseSet.Count - 1; i >= 0; i--)
        {
            entity.EntityID = entityId;
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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        foreach (var entityId in idsToUpdate)
        {
            if(!((uint)entityId < (uint)map.Length))
            {
                continue;
            }
            
            entity.EntityID = entityId;

            int denseIndex = map[entityId];

            // ids in idsToUpdate are not guarenteed to be in this set
            if (denseIndex <= 0)
            {
                continue;
            }

            Unsafe.Add(ref component, denseIndex).Update(entity, uniform);
        }
    }
}


/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class EntityUniformUpdateRunner<TComp, TUniform, TArg> : IRunner
    where TComp : IEntityUniformComponent<TUniform, TArg>
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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        Span<Bitset> bitsets = b.SparseBitsetSpan();
        // TODO: double check that the jit register promotes this.
        // This needs to stay in a ymm register on x86
        Bitset includeBits = SparseIncludeBits;

        for (int i = 0; i < length; i++)
        {
            entityIds.SetEntity(ref entity);
            if (Component<TArg>.IsSparseComponent && (uint)i < (uint)bitsets.Length)
            {
                Bitset.AssertHasSparseComponents(ref bitsets[i], ref includeBits);
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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = sparseSet.Count - 1; i >= 0; i--)
        {
            entity.EntityID = entityId;
            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, SparseIncludeBits)
                : entity.GetCachedLookup(world);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        foreach (var entityId in idsToUpdate)
        {
            if (!((uint)entityId < (uint)map.Length))
                continue;
            int denseIndex = map[entityId];
            if (denseIndex <= 0)
                continue;

            entity.EntityID = entityId;
            var entityData = Component<TArg>.IsSparseComponent
                ? entity.GetCachedLookupAndAssertSparseComponent(world, SparseIncludeBits)
                : entity.GetCachedLookup(world);

            ref TArg arg = ref Component<TArg>.IsSparseComponent
                ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
                : ref entityData.Get<TArg>();

            Unsafe.Add(ref component, denseIndex).Update(entity, uniform, ref arg);
        }
    }
}