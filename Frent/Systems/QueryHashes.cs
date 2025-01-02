using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("        .With<T>()", "|        .With<T$>()\n|")]
public static class QueryHashes<T>
{
    public static readonly int Hash = QueryHash.New()
        .With<T>()
        .ToHashCode();
}

public static class QueryHashes
{
    //https://xkcd.com/221/
    public static readonly int Hash = 4;
}