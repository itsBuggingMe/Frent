using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

internal class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IUpdateComponent
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>());
    internal struct Action : IQuery<TComp>
    {
        public void Run(ref TComp t) => t.Update();
    }
}

public class UpdateRunnerFactory<TComp> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUpdateComponent
{
    public object Create() => new Update<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IUpdateComponent<TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal struct Action : IQuery<TComp, TArg>
    {
        public void Run(ref TComp c, ref TArg t1) => c.Update(ref t1);
    }
}

[Variadic(GenArgFrom, GenArgPattern, 15)]
public class UpdateRunnerFactory<TComp, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUpdateComponent<TArg>
{
    public object Create() => new Update<TComp, TArg>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp, TArg>();
}