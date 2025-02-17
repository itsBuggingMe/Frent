using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Delegates for executing a functions on a <see cref="Query"/>
/// </summary>
[Variadic("T>", "|T$, |>")]
[Variadic("ref T comp1", "|ref T$ comp$, |")]
public static partial class QueryDelegates
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public delegate void Query<T>(ref T comp1);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}