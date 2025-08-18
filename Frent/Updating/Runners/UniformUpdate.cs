using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class UniformUpdateRunner<TComp, TUniform> : IRunner
    where TComp : IUniformComponent<TUniform>
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(uniform);

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = idsToUpdate.Length - 1; i >= 0; i--)
        {
            component.Update(uniform);
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class UniformUpdateRunner<TComp, TUniform, TArg> : IRunner
    where TComp : IUniformComponent<TUniform, TArg>
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
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);

            if (Component<TArg>.IsSparseComponent)
            {
                if (!((uint)entity.EntityID < (uint)sparseArgArray.Length)) goto NullRefException;
                int index = sparseArgArray[entity.EntityID];
                if (index < 0) goto NullRefException;
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(uniform, ref arg);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);

            if (!Component<TArg>.IsSparseComponent) arg = ref Unsafe.Add(ref arg, 1);
        }

        return;
    NullRefException: Unsafe.NullRef<int>() = 0;
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref int entityId = ref MemoryMarshal.GetReference(idsToUpdate);
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);

        // folded   
        ref TArg sparseFirst = ref IRunner.InitSparse<TArg>(ref first, out Span<int> sparseArgArray);

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = idsToUpdate.Length - 1; i >= 0; i--, entityId = ref Unsafe.Add(ref entityId, 1))
        {
            entity.EntityID = entityId;
            var entityData = entity.GetCachedLookup(world);

            ref TArg arg = ref Unsafe.NullRef<TArg>();
            if (Component<TArg>.IsSparseComponent) // folded
            {
                if (!((uint)entity.EntityID < (uint)sparseArgArray.Length)) goto NullRefException;
                int index = sparseArgArray[entity.EntityID];
                if (index < 0) goto NullRefException;
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }
            else
            {
                nint index = entityData.ComponentDataIndex<TArg>();
                arg = ref entityData.Get<TArg>(index);
            }

            component.Update(uniform, ref arg);
        }

        return;
    NullRefException: Unsafe.NullRef<int>() = 0;
    }
}