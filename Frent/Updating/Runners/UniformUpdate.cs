using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

internal class UniformUpdate<TComp, TUniform> : ComponentRunnerBase<UniformUpdate<TComp, TUniform>, TComp>
    where TComp : IUniformUpdateComponent<TUniform>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpan<Action>
        (b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetComponentSpan<TComp>());
    internal struct Action : IQuery<TComp>
    {
        public TUniform Uniform;
        public void Run(ref TComp t) => t.Update(in Uniform);
    }
}

public class UniformUpdateRunnerFactory<TComp, TUniform> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUniformUpdateComponent<TUniform>
{
    public object Create() => new UniformUpdate<TComp, TUniform>();
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class UniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<UniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IUniformUpdateComponent<TUniform, TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpan<Action>
        (b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());

    internal struct Action : IQuery<TComp, TArg>
    {
        public TUniform Uniform;
        public void Run(ref TComp c, ref TArg t1) => c.Update(in Uniform, ref t1);
    }
}

[Variadic(GenArgFrom, GenArgPattern, 15)]
public class UniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUniformUpdateComponent<TUniform, TArg>
{
    public object Create() => new UniformUpdate<TComp, TUniform, TArg>();
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform, TArg>();
}