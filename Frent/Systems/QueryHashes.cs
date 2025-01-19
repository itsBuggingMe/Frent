using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Immutable;

namespace Frent.Systems;

[Variadic("        Rule.HasComponent(Component<T>.ID),", "|        Rule.HasComponent(Component<T$>.ID),\n|")]
[Variadic("            Rule.NotComponent(Component<N>.ID),", "|            Rule.NotComponent(Component<N$>.ID),\n|")]
[Variadic("<T>", "<|T$, |>")]
[Variadic("<N>", "<|N$, |>")]
public readonly struct With<T> : IConstantQueryHashProvider
{
    private static readonly ImmutableArray<Rule> _rules = MemoryHelpers.ReadOnlySpanToImmutableArray(
    [
        Rule.HasComponent(Component<T>.ID),
    ]);

    private static readonly int _hashCache = QueryHash.New(_rules).ToHashCodeIncludeDisable();
    public ImmutableArray<Rule> Rules => _rules;
    public int ToHashCode() => _hashCache;

    public readonly struct ButNot<N> : IConstantQueryHashProvider
    {
        private static readonly ImmutableArray<Rule> _rulesNot = MemoryHelpers.ConcatImmutable(_rules,
        [
            Rule.NotComponent(Component<N>.ID),
        ]);

        private static readonly int _hashCache = QueryHash.New(_rulesNot).ToHashCodeIncludeDisable();
        public ImmutableArray<Rule> Rules => _rulesNot;
        public int ToHashCode() => _hashCache;


        public readonly struct IncludeDisabled : IConstantQueryHashProvider
        {
            private static readonly int _hashCache = QueryHash.New(_rulesNot).ToHashCodeIncludeDisable();
            public ImmutableArray<Rule> Rules => _rulesNot;
            public int ToHashCode() => _hashCache;
        }
    }


    public readonly struct IncludeDisabled : IConstantQueryHashProvider
    {
        private static readonly int _hashCache = QueryHash.New(_rules).ToHashCodeIncludeDisable();
        public ImmutableArray<Rule> Rules => _rules;
        public int ToHashCode() => _hashCache;
    }
}