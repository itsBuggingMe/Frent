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
        Span<TComp> arr = AsSpan(b.EntityCount);
        for(int i = 0; i < arr.Length; i++)
        {
            arr[i].Update();
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

[Variadic(GetSpanFrom, GetSpanPattern)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg> : ComponentStorage<TComp>
    where TComp : IComponent<TArg>
{
    internal override void Run(World world, Archetype b)
    {
        Span<TComp> comps = AsSpan(b.EntityCount);
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(ref arg[i]);
        }    
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(GenArgFrom, GenArgPattern)]
public class UpdateRunnerFactory<TComp, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new Update<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new Update<TComp, TArg>();
}