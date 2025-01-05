﻿using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

internal class EntityUpdate<TComp> : ComponentRunnerBase<EntityUpdate<TComp>, TComp>
    where TComp : IEntityUpdateComponent
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetEntitySpan(), b.GetComponentSpan<TComp>());
    internal struct Action : IQueryEntity<TComp>
    {
        public void Run(Entity entity, ref TComp t) => t.Update(entity);
    }
}
public class EntityUpdateRunnerFactory<TComp> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUpdateComponent
{
    public object Create() => new EntityUpdate<TComp>();
    public IComponentRunner<TComp> CreateStronglyTyped() => new EntityUpdate<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUpdate<TComp, TArg> : ComponentRunnerBase<EntityUpdate<TComp, TArg>, TComp>
    where TComp : IEntityUpdateComponent<TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetEntitySpan(), b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal struct Action : IQueryEntity<TComp, TArg>
    {
        public void Run(Entity entity, ref TComp c, ref TArg t1) => c.Update(entity, ref t1);
    }
}

[Variadic(GenArgFrom, GenArgPattern, 15)]
public class EntityUpdateRunnerFactory<TComp, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUpdateComponent<TArg>
{
    public object Create() => new EntityUpdate<TComp, TArg>();
    public IComponentRunner<TComp> CreateStronglyTyped() => new EntityUpdate<TComp, TArg>();
}