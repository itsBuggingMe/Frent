using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("<T>", "<|T$, |>")]
[Variadic("        .AddRule(default(T).Rule)", "|        .AddRule(default(T$).Rule)\n|")]
[Variadic("    where T : struct, IRuleProvider", "|    where T$ : struct, IRuleProvider\n|")]
internal static class QueryHashCache<T>
    where T : struct, IRuleProvider
{
    public static readonly int Value = new QueryHash()
        .AddRule(default(T).Rule)
        .ToHashCode();
}
