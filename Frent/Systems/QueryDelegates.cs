using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("T>", "|T$, |>")]
[Variadic("ref T comp1", "|ref T$ comp$, |")]
public static partial class QueryDelegates
{
    public delegate void Query<T>(ref T comp1);
    public delegate void QueryEntity<T>(Entity entity, ref T comp1);
    public delegate void QueryEntityUniform<TUniform, T>(Entity entity, TUniform uniform, ref T comp1);
    public delegate void QueryUniform<TUniform, T>(TUniform uniform, ref T comp1);
}
partial class QueryDelegates
{
    public delegate void QueryEntityOnly(Entity entity);
}