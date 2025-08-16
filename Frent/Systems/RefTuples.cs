using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;
#pragma warning disable CS1591 // for Item1 fields

/// <summary>
/// A tuple of multiple references.
/// </summary>
/// <variadic />
[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Ref<T> Item1;", "|    public Ref<T$> Item$;\n|")]
[Variadic("out Ref<T> @ref", "|out Ref<T$> @ref$, |")]
[Variadic("        @ref = Item1;", "|        @ref$ = Item$;\n|")]
public ref struct RefTuple<T>
{
    public Ref<T> Item1;

    /// <summary>
    /// Allows tuple deconstruction syntax to be used.
    /// </summary>
    public void Deconstruct(out Ref<T> @ref)
    {
        @ref = Item1;
    }
}

/// <summary>
/// A tuple of multiple references with an <see cref="Entity"/>.
/// </summary>
/// <variadic />
[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Ref<T> Item1;", "|    public Ref<T$> Item$;\n|")]
[Variadic("out Ref<T> @ref", "|out Ref<T$> @ref$, |")]
[Variadic("        @ref = Item1;", "|        @ref$ = Item$;\n|")]
public ref struct EntityRefTuple<T>
{
    /// <summary>
    /// The current <see cref="Entity"/>; the components in this tuple belong to this <see cref="Entity"/>.
    /// </summary>
    public Entity Entity;
    public Ref<T> Item1;

    /// <summary>
    /// Allows tuple deconstruction syntax to be used.
    /// </summary>
    public void Deconstruct(out Entity entity, out Ref<T> @ref)
    {
        entity = Entity;
        @ref = Item1;
    }
}

/// <summary>
/// A tuple of a chunk of entities and their components.
/// </summary>
/// <variadic />
[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Span<T> Span;", "|    public Span<T$> Span$;\n|")]
[Variadic("out Span<T> @comp1", "|out Span<T$> @comp$, |")]
[Variadic("        @comp1 = Span;", "|        @comp$ = Span$;\n|")]
public ref struct ChunkTuple<T>
{
    /// <summary>
    /// An enumerator that can be used to enumerate individual <see cref="Entity"/> instances.
    /// </summary>
    public EntityEnumerator Entities;
    public Span<T> Span;

    /// <summary>
    /// Allows tuple deconstruction syntax to be used.
    /// </summary>
    public void Deconstruct(out Span<T> @comp1)
    {
        @comp1 = Span;
    }
}