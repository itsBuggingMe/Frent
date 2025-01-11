using Frent.Variadic.Generator;
using Frent.Core;

namespace Frent.Systems;

[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("        .AddRule(Rule.HasComponent(Component<T>.ID))", "|        .AddRule(Rule.HasComponent(Component<T$>.ID))\n|")]
internal static class QueryHashes<T>
{
    public static readonly int Hash = QueryHash.New()
        .AddRule(Rule.HasComponent(Component<T>.ID))
        .ToHashCode();
}

internal static class QueryHashes
{
    //https://xkcd.com/221/
    public static readonly int Hash = 4;
}