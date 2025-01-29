using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Immutable;

namespace Frent.Systems;

/// <summary>
/// Defines a set of rules that filter a query.
/// </summary>
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

    private static readonly int _hashCache = QueryHash.New(_rules).ToHashCodeExcludeDisable();
    /// <summary>
    /// The set of rules that this filters.
    /// </summary>
    public ImmutableArray<Rule> Rules => _rules;
    /// <inheritdoc/>
    public int ToHashCode() => _hashCache;

    /// <summary>
    /// Defines a set of rules that filter a query.
    /// </summary>
    public readonly struct ButNot<N> : IConstantQueryHashProvider
    {
        private static readonly ImmutableArray<Rule> _rulesNot = MemoryHelpers.ConcatImmutable(_rules,
        [
            Rule.NotComponent(Component<N>.ID),
        ]);

        private static readonly int _hashCache = QueryHash.New(_rulesNot).ToHashCodeExcludeDisable();
        /// <summary>
        /// The set of rules that this filters.
        /// </summary>
        public ImmutableArray<Rule> Rules => _rulesNot;

        /// <inheritdoc/>
        public int ToHashCode() => _hashCache;

        /// <summary>
        /// Includes entities with the <see cref="Disable"/> tag
        /// </summary>
        public readonly struct IncludeDisabled : IConstantQueryHashProvider
        {
            private static readonly int _hashCache = QueryHash.New(_rulesNot).ToHashCodeIncludeDisable();
            /// <summary>
            /// The set of rules that this filters.
            /// </summary>
            public ImmutableArray<Rule> Rules => _rulesNot;
            /// <inheritdoc/>
            public int ToHashCode() => _hashCache;
        }
    }

    /// <summary>
    /// Includes entities with the <see cref="Disable"/> tag
    /// </summary>
    public readonly struct IncludeDisabled : IConstantQueryHashProvider
    {
        private static readonly int _hashCache = QueryHash.New(_rules).ToHashCodeIncludeDisable();
        /// <summary>
        /// The set of rules that this filters.
        /// </summary>
        public ImmutableArray<Rule> Rules => _rules;
        /// <summary>
        /// The set of rules that this filters.
        /// </summary>
        public int ToHashCode() => _hashCache;
    }
}