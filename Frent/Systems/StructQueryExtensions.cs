using Frent.Buffers;
using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("archetype.GetComponentSpan<T>()", "|archetype.GetComponentSpan<T$>(), |")]
[Variadic("ref arg", "|ref arg$, |")]
[Variadic("ref T arg", "|ref T$ arg$, |")]
[Variadic("T>", "|T$, |>")]
public static partial class StructQueryExtensions
{
    public static void Inline<TAction, T>(this Query query, TAction action)
        where TAction : struct, IAction<T>
    {
        foreach (RefTuple<T> tuple in query.Enumerate<T>())
        {
            action.Run(ref tuple.Item1.Value);
        }
    }

    public static void InlineEntity<TAction, T>(this Query query, TAction action)
        where TAction : struct, IEntityAction<T>
    {
        foreach((Entity entity, Ref<T> @ref) in query.EnumerateWithEntities<T>())
        {
            action.Run(entity, ref @ref.Value);
        }
    }

    public static void InlineUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponents(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new UniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetComponentSpan<T>());
        }
    }

    internal struct UniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IAction<T>
        where TAction : struct, IUniformAction<TUniform, T>
    {
        public void Run(ref T arg) => action.Run(uniform, ref arg);
    }

    public static void InlineEntityUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IEntityUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponentsWithEntity(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new EntityUniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    internal struct EntityUniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IEntityAction<T>
        where TAction : struct, IEntityUniformAction<TUniform, T>
    {
        public void Run(Entity entity, ref T arg) => action.Run(entity, uniform, ref arg);
    }
}

static partial class StructQueryExtensions
{
    public static void InlineEntity<TAction>(this Query query, TAction action)
        where TAction : struct, IEntityAction
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers.EnumerateComponentsWithEntity(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                archetype.GetEntitySpan());
        }
    }
}