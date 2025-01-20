using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

internal class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IComponent
{
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp>.EnumerateComponents(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetComponentSpan<TComp>());

    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp>.EnumerateComponents(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetComponentSpan<TComp>());

    internal struct Action : IAction<TComp>
    {
        public void Run(ref TComp t) => t.Update();
    }
}


public class UpdateRunnerFactory<TComp> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IComponent
{
    public object Create() => new Update<TComp>();
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IComponent<TArg>
{
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp, TArg>.EnumerateComponents(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());

    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp, TArg>.EnumerateComponents(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());

    internal struct Action : IAction<TComp, TArg>
    {
        public void Run(ref TComp c, ref TArg t1) => c.Update(ref t1);
    }
}


[Variadic(GenArgFrom, GenArgPattern, 15)]
public class UpdateRunnerFactory<TComp, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IComponent<TArg>
{
    public object Create() => new Update<TComp, TArg>();
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp, TArg>();
}