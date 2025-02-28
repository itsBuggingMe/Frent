using System.Runtime.CompilerServices;
using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class EntityUpdate<TComp> : ComponentStorage<TComp>
    where TComp : IEntityComponent
{
    internal override void Run(World world, Archetype b)
    {
        ref EntityIDOnly entityIds = ref b.GetEntityDataReference();
        ref TComp comp = ref GetComponentStorageDataReference();

        Entity entity = world.DefaultWorldEntity;

        for (int i = b.EntityCount; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);
            comp.Update(entity);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }
    
    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class EntityUpdateRunnerFactory<TComp> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IEntityComponent
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp>();
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUpdate<TComp, TArg> : ComponentStorage<TComp>
    where TComp : IEntityComponent<TArg>
{
    internal override void Run(World world, Archetype b)
    {
        ref EntityIDOnly entityIds = ref b.GetEntityDataReference();
        ref TComp comp = ref GetComponentStorageDataReference();

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        Entity entity = world.DefaultWorldEntity;

        for (int i = b.EntityCount; i >= 0; i--)
        {
            entityIds.SetEntity(ref entity);
            comp.Update(entity, ref arg);

            entityIds = ref Unsafe.Add(ref entityIds, 1);
            comp = ref Unsafe.Add(ref comp, 1);

            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b)
        => throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(TArgFrom, TArgPattern)]
public class EntityUpdateRunnerFactory<TComp, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IEntityComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp, TArg>();
}