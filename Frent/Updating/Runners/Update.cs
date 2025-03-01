using System.Runtime.CompilerServices;
using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class Update<TComp>(int cap) : ComponentStorage<TComp>(cap)
    where TComp : IComponent
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        for(int i = b.EntityCount - 1; i >= 0; i--)
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
    ComponentStorageBase IComponentStorageBaseFactory.Create(int capacity) => new Update<TComp>(capacity);
    IDTable IComponentStorageBaseFactory.CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped(int capacity) => new Update<TComp>(capacity);
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg>(int cap) : ComponentStorage<TComp>(cap)
    where TComp : IComponent<TArg>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
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
    ComponentStorageBase IComponentStorageBaseFactory.Create(int capacity) => new Update<TComp, TArg>(capacity);
    IDTable IComponentStorageBaseFactory.CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped(int capacity) => new Update<TComp, TArg>(capacity);
}