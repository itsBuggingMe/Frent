using System.Runtime.CompilerServices;
using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class UniformUpdate<TComp, TUniform> : ComponentStorage<TComp>
    where TComp : IUniformComponent<TUniform>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount; i >= 0; i--)
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
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform>();
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class UniformUpdate<TComp, TUniform, TArg> : ComponentStorage<TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    internal override void Run(World world, Archetype b)
    {
        ref TComp comp = ref GetComponentStorageDataReference();

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount; i >= 0; i--)
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
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform, TArg>();
}