using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;


[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Ref<T> Item1;", "|    public Ref<T$> Item$;\n|")]
[Variadic("out Ref<T> @ref", "|out Ref<T$> @ref$, |")]
[Variadic("        @ref = Item1;", "|        @ref$ = Item$;\n|")]
public ref struct RefTuple<T>
{
    public Ref<T> Item1;
    public void Deconstruct(out Ref<T> @ref)
    {
        @ref = Item1;
    }
}

[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Ref<T> Item1;", "|    public Ref<T$> Item$;\n|")]
[Variadic("out Ref<T> @ref", "|out Ref<T$> @ref$, |")]
[Variadic("        @ref = Item1;", "|        @ref$ = Item$;\n|")]
public ref struct EntityRefTuple<T>
{
    public Entity Entity;
    public Ref<T> Item1;
    public void Deconstruct(out Entity entity, out Ref<T> @ref)
    {
        entity = Entity;
        @ref = Item1;
    }
}

[Variadic("Tuple<T>", "Tuple<|T$, |>")]
[Variadic("    public Span<T> Item1;", "|    public Span<T$> Item$;\n|")]
[Variadic("out Span<T> @comp1", "|out Span<T$> @comp$, |")]
[Variadic("        @ref = Item1;", "|        @ref$ = Item$;\n|")]
public ref struct ChunkTuple<T>
{
    public Span<T> Item1;
    public void Deconstruct(out Span<T> @comp1)
    {
        @comp1 = Item1;
    }
}