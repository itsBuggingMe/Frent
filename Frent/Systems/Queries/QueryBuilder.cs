using Frent.Core;

namespace Frent.Systems.Queries;

/// <inheritdoc cref="IQueryBuilder"/>
public readonly struct QueryBuilder(World world) : IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    public World World { get; init; } = world;
    /// <inheritdoc cref="IQueryBuilder"/>
    public void AddRules(List<Rule> rules) { }

    /// <summary>
    /// Excludes entities with the tag <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of excludes.</typeparam>
    public readonly QueryUntagged<N, QueryBuilder> Untagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the tag <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of include.</typeparam>
    public readonly QueryTagged<N, QueryBuilder> Tagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the component <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of component to include.</typeparam>
    public readonly QueryWith<N, QueryBuilder> With<N>() => new(World);
    /// <summary>
    /// Excludes entities with the component <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of component to exclude.</typeparam>
    public readonly QueryWithout<N, QueryBuilder> Without<N>() => new(World);
    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly Query Build() => World.BuildQuery<QueryBuilder>();
}

/// <inheritdoc cref="IQueryBuilder"/>
public struct QueryWith<T, TRest>(World world) : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    public World World { get; init; } = world;

    /// <inheritdoc cref="IQueryBuilder"/>
    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.HasComponent(Component<T>.ID));
        default(TRest).AddRules(rules);
    }

    /// <summary>
    /// Excludes entities with the tag <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of excludes.</typeparam>
    public readonly QueryUntagged<N, QueryWith<T, TRest>> Untagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the tag <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of include.</typeparam>
    public readonly QueryTagged<N, QueryWith<T, TRest>> Tagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the component <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of component to include.</typeparam>
    public readonly QueryWith<N, QueryWith<T, TRest>> With<N>() => new(World);
    /// <summary>
    /// Excludes entities with the component <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of component to exclude.</typeparam>
    public readonly QueryWithout<N, QueryWith<T, TRest>> Without<N>() => new(World);

    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly Query Build() => World.BuildQuery<QueryWith<T, TRest>>();
}

/// <inheritdoc cref="IQueryBuilder"/>
public readonly struct QueryWithout<T, TRest>(World world) : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    public World World { get; init; } = world;

    /// <inheritdoc cref="IQueryBuilder"/>
    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.NotComponent(Component<T>.ID));
        default(TRest).AddRules(rules);
    }

    /// <summary>
    /// Excludes entities with the tag <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of excludes.</typeparam>
    public readonly QueryUntagged<N, QueryWithout<T, TRest>> Untagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the tag <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of include.</typeparam>
    public readonly QueryTagged<N, QueryWithout<T, TRest>> Tagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the component <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of component to include.</typeparam>
    public readonly QueryWith<N, QueryWithout<T, TRest>> With<N>() => new(World);
    /// <summary>
    /// Excludes entities with the component <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of component to exclude.</typeparam>
    public readonly QueryWithout<N, QueryWithout<T, TRest>> Without<N>() => new(World);

    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly Query Build() => World.BuildQuery<QueryWithout<T, TRest>>();
}

/// <inheritdoc cref="IQueryBuilder"/>
public readonly struct QueryTagged<T, TRest>(World world) : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    public World World { get; init; } = world;

    /// <inheritdoc cref="IQueryBuilder"/>
    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.HasTag(Tag<T>.ID));
        default(TRest).AddRules(rules);
    }

    /// <summary>
    /// Excludes entities with the tag <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of excludes.</typeparam>
    public readonly QueryUntagged<N, QueryTagged<T, TRest>> Untagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the tag <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of include.</typeparam>
    public readonly QueryTagged<N, QueryTagged<T, TRest>> Tagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the component <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of component to include.</typeparam>
    public readonly QueryWith<N, QueryTagged<T, TRest>> With<N>() => new(World);
    /// <summary>
    /// Excludes entities with the component <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of component to exclude.</typeparam>
    public readonly QueryWithout<N, QueryTagged<T, TRest>> Without<N>() => new(World);

    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly Query Build() => World.BuildQuery<QueryTagged<T, TRest>>();
}

/// <inheritdoc cref="IQueryBuilder"/>
public readonly struct QueryUntagged<T, TRest>(World world) : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    public World World { get; init; } = world;

    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.NotTag(Tag<T>.ID));
        default(TRest).AddRules(rules);
    }

    /// <summary>
    /// Excludes entities with the tag <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of excludes.</typeparam>
    public readonly QueryUntagged<N, QueryUntagged<T, TRest>> Untagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the tag <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of tag of include.</typeparam>
    public readonly QueryTagged<N, QueryUntagged<T, TRest>> Tagged<N>() => new(World);
    /// <summary>
    /// Includes entities with the component <typeparamref name="N"/> in this query.
    /// </summary>
    /// <typeparam name="N">The type of component to include.</typeparam>
    public readonly QueryWith<N, QueryUntagged<T, TRest>> With<N>() => new(World);
    /// <summary>
    /// Excludes entities with the component <typeparamref name="N"/> from this query.
    /// </summary>
    /// <typeparam name="N">The type of component to exclude.</typeparam>
    public readonly QueryWithout<N, QueryUntagged<T, TRest>> Without<N>() => new(World);


    /// <inheritdoc cref="IQueryBuilder"/>
    public readonly Query Build() => World.BuildQuery<QueryUntagged<T, TRest>>();
}