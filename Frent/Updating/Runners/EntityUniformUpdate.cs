using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class EntityUniformUpdateRunner<TComp, TUniform> : IRunner
    where TComp : IEntityUniformComponent<TUniform>
{
    ComponentID IRunner.ComponentID => Component<TComp>.ID;

    void IRunner.Run(Array array, Archetype b, World world)
    {
        if (Component<TComp>.IsSparseComponent)
            throw new NotSupportedException();

        ref EntityIDOnly entityIds = ref b.GetEntityDataReference();
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(array);

        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);
            comp.Update(entity, uniform);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        if (Component<TComp>.IsSparseComponent)
            throw new NotSupportedException();

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
        if (!Component<TComp>.IsSparseComponent)
            throw new NotSupportedException();

        ref int entityId = ref sparseSet.GetEntityIDsDataReference();
        ref TComp component = ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<TComp>>(sparseSet).GetComponentDataReference();
        Entity entity = world.DefaultWorldEntity;
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = sparseSet.Count - 1; i >= 0; i--)
        {
            component.Update(entity, uniform);
        }
    }
}


/// <inheritdoc cref="GenerationServices"/>
[Variadic(nameof(IRunner))]
public class EntityUniformUpdateRunner<TComp, TUniform, TArg> : IRunner
    where TComp : IEntityUniformComponent<TUniform, TArg>
{
    ComponentID IRunner.ComponentID => Component<TComp>.ID;

    //maybe field acsesses can be optimzed???
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
                if (!((uint)entity.EntityID < (uint)sparseArgArray.Length)) continue;
                int index = sparseArgArray[i];
                if(index < 0) continue;
                arg = ref Unsafe.Add(ref sparseFirst, index);
            }

            comp.Update(entity, uniform, ref arg);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);

            if(!Component<TArg>.IsSparseComponent) arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    void IRunner.RunSparse(ComponentSparseSetBase sparseSet, World world, Span<int> idsToUpdate)
    {
        if (!Component<TComp>.IsSparseComponent)
            throw new NotSupportedException();

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
                if (!((uint)entity.EntityID < (uint)sparseArgArray.Length)) continue;
                arg = ref Unsafe.Add(ref sparseFirst, sparseArgArray[entity.EntityID]);
            }
            else
            {
                nint index = entityData.ComponentDataIndex<TArg>();
                if (index == 0) continue;
                arg = ref entityData.Get<TArg>(index);
            }
            
            component.Update(entity, uniform, ref arg);
        }
    }
}