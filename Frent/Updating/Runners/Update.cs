using System.Runtime.CompilerServices;
using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class Update<TComp> : ComponentStorage<TComp>
    where TComp : IComponent
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        for(int i = b.EntityCount; i >= 0; i--)
        {
            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class UpdateRunnerFactory<TComp> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IComponent
{
    /// <inheritdoc/>
    public object Create() => new Update<TComp>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new Update<TComp>();
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg> : ComponentStorage<TComp>
    where TComp : IComponent<TArg>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        for (int i = b.EntityCount; i >= 0; i--)
        {
            comp.Update(ref arg);

            comp = ref Unsafe.Add(ref comp, 1);

            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(TArgFrom, TArgPattern)]
public class UpdateRunnerFactory<TComp, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new Update<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new Update<TComp, TArg>();
}