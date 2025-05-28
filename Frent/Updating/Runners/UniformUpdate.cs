using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class UniformUpdate<TComp, TUniform>(int cap) : ComponentStorage<TComp>(cap)
    where TComp : IUniformComponent<TUniform>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(uniform);

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    internal override void Run(World world, Archetype b, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref GetComponentStorageDataReference(), start);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(uniform);

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class UniformUpdateRunnerFactory<TComp, TUniform> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IUniformComponent<TUniform>
{
    ComponentStorageRecord IComponentStorageBaseFactory.Create(int capacity) => new UniformUpdate<TComp, TUniform>(capacity);
    IDTable IComponentStorageBaseFactory.CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped(int capacity) => new UniformUpdate<TComp, TUniform>(capacity);
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(GetComponentRefWithStartFrom, GetComponentRefWithStartPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class UniformUpdate<TComp, TUniform, TArg>(int capacity) : ComponentStorage<TComp>(capacity)
    where TComp : IUniformComponent<TUniform, TArg>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(uniform, ref arg);

            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    internal override void Run(World world, Archetype b, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref GetComponentStorageDataReference(), start);

        ref TArg arg = ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(uniform, ref arg);

            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(TArgFrom, TArgPattern)]
public class UniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    ComponentStorageRecord IComponentStorageBaseFactory.Create(int capacity) => new UniformUpdate<TComp, TUniform, TArg>(capacity);
    IDTable IComponentStorageBaseFactory.CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped(int capacity) => new UniformUpdate<TComp, TUniform, TArg>(capacity);
}