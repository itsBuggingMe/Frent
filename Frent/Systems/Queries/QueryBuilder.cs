using Frent.Collections;
using Frent.Core;

namespace Frent.Systems.Queries;

public struct QueryBuilder(World world) : IQueryBuilder
{
    public World? World => world;
    public void AddRules(List<Rule> rules) { }
    public QueryWith<T, QueryBuilder> With<T>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryBuilder>> With<T1, T2>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryWith<T3, QueryBuilder>>> With<T1, T2, T3>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryWith<T3, QueryWith<T4, QueryBuilder>>>> With<T1, T2, T3, T4>() => new() { World = World };

    public readonly Query Build() => World?.BuildQuery<QueryBuilder>() ?? throw new InvalidOperationException();
}

public struct QueryWith<T, TRest> : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    public World? World { get; init; }

    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.HasComponent(Component<T>.ID));
        default(TRest).AddRules(rules);
    }   

    public QueryWith<T, QueryWith<T, TRest>> With<T>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryWith<T, TRest>>> With<T1, T2>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryWith<T3, QueryWith<T, TRest>>>> With<T1, T2, T3>() => new() { World = World };
    public QueryWith<T1, QueryWith<T2, QueryWith<T3, QueryWith<T4, QueryWith<T, TRest>>>>> With<T1, T2, T3, T4>() => new() { World = World };

    public QueryWithout<T, QueryWith<T, TRest>> Without<T>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWith<T, TRest>>> Without<T1, T2>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWithout<T3, QueryWith<T, TRest>>>> Without<T1, T2, T3>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWithout<T3, QueryWithout<T4, QueryWith<T, TRest>>>>> Without<T1, T2, T3, T4>() => new() { World = World };

    public readonly Query Build() => World?.BuildQuery<QueryWith<T, TRest>>() ?? throw new InvalidOperationException();
}

public struct QueryWithout<T, TRest> : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    public World? World { get; init; }


    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.NotComponent(Component<T>.ID));
        default(TRest).AddRules(rules);
    }

    public QueryWithout<T, QueryWithout<T, TRest>> Without<T>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWithout<T, TRest>>> Without<T1, T2>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWithout<T3, QueryWithout<T, TRest>>>> Without<T1, T2, T3>() => new() { World = World };
    public QueryWithout<T1, QueryWithout<T2, QueryWithout<T3, QueryWithout<T4, QueryWithout<T, TRest>>>>> Without<T1, T2, T3, T4>() => new() { World = World };

    public QueryTagged<T, QueryWithout<T, TRest>> Tagged<T>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryWithout<T, TRest>>> Tagged<T1, T2>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryTagged<T3, QueryWithout<T, TRest>>>> Tagged<T1, T2, T3>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryTagged<T3, QueryTagged<T4, QueryWithout<T, TRest>>>>> Tagged<T1, T2, T3, T4>() => new() { World = World };

    public readonly Query Build() => World?.BuildQuery<QueryWithout<T, TRest>>() ?? throw new InvalidOperationException();
}

public struct QueryTagged<T, TRest> : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    public World? World { get; init; }

    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.HasTag(Tag<T>.ID));
        default(TRest).AddRules(rules);
    }

    public QueryTagged<T, QueryTagged<T, TRest>> Tagged<T>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryTagged<T, TRest>>> Tagged<T1, T2>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryTagged<T3, QueryTagged<T, TRest>>>> Tagged<T1, T2, T3>() => new() { World = World };
    public QueryTagged<T1, QueryTagged<T2, QueryTagged<T3, QueryTagged<T4, QueryTagged<T, TRest>>>>> Tagged<T1, T2, T3, T4>() => new() { World = World };

    public QueryUntagged<T, QueryTagged<T, TRest>> Untagged<T>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryTagged<T, TRest>>> Untagged<T1, T2>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryUntagged<T3, QueryTagged<T, TRest>>>> Untagged<T1, T2, T3>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryUntagged<T3, QueryUntagged<T4, QueryTagged<T, TRest>>>>> Untagged<T1, T2, T3, T4>() => new() { World = World };
    
    public readonly Query Build() => World?.BuildQuery<QueryTagged<T, TRest>>() ?? throw new InvalidOperationException();
}

public struct QueryUntagged<T, TRest> : IQueryBuilder
    where TRest : struct, IQueryBuilder
{
    public World? World { get; init; }

    public void AddRules(List<Rule> rules)
    {
        rules.Add(Rule.NotTag(Tag<T>.ID));
        default(TRest).AddRules(rules);
    }

    public QueryUntagged<T, QueryUntagged<T, TRest>> Untagged<T>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryUntagged<T, TRest>>> Untagged<T1, T2>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryUntagged<T3, QueryUntagged<T, TRest>>>> Untagged<T1, T2, T3>() => new() { World = World };
    public QueryUntagged<T1, QueryUntagged<T2, QueryUntagged<T3, QueryUntagged<T4, QueryUntagged<T, TRest>>>>> Untagged<T1, T2, T3, T4>() => new() { World = World };

    public readonly Query Build() => World?.BuildQuery<QueryUntagged<T,TRest>>() ?? throw new InvalidOperationException();
}