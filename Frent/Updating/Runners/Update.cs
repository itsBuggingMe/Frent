using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class UpdateRunner<TComp> : IRunner
    where TComp : IComponent
{
    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world, ReadOnlySpan<int> idsToUpdate)
    {
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();

        for (int i = idsToUpdate.Length - 1; i >= 0; i--)
        {
            component.Update();
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class UpdateRunner<TComp, TArg> : IRunner
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

        Entity entity = world.DefaultWorldEntity;

        for (int i = length - 1; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);

            if (Component<TArg>.IsSparseComponent)
            {
                if (!((uint)entity.EntityID < (uint)sparseArgArray.Length)) goto NullRefException;
                int index = sparseArgArray[i];
                if (index < 0) goto NullRefException;
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(ref arg);

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

            component.Update(ref arg);
        }

        return;
    NullRefException: Unsafe.NullRef<int>() = 0;
    }
}