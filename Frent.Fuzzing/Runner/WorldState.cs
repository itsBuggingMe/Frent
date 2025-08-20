using System;
using System.Runtime.CompilerServices;
using Frent.Core;

namespace Frent.Fuzzing.Runner;

internal partial class WorldState : IDisposable
{
    // record keeping
    private readonly Random _random;
    private readonly List<StepRecord> _actions = [];
    private int _steps;
    private int _seed;

    // actual state
    private readonly World _worldState;

    // expected state
    private Dictionary<Entity, List<ComponentHandle>> _componentValues = [];
    private IEnumerable<Entity> Entities => _componentValues.Select(kvp => kvp.Key);
    private List<Entity> _dead = [];

    public WorldState(int seed) : this()
    {
        _seed = seed;
        _random = new Random(seed);
        _worldState = new();
    }

    public void Advance()
    {
        WorldActions thisAction = WorldActionsHelper.SelectWeightedAction(_random);

        _actions.Add(thisAction switch
        {
            WorldActions.CreateGeneric => CreateGeneric(),
            WorldActions.CreateHandles => CreateHandles(),
            WorldActions.CreateObjects => CreateObjects(),
            WorldActions.Delete => Delete(),
            WorldActions.AddGeneric => AddGeneric(),
            WorldActions.AddHandles => AddHandles(),
            WorldActions.AddObject => AddObject(),
            WorldActions.AddAs => AddAs(),
            WorldActions.RemoveGeneric => RemoveGeneric(),
            WorldActions.RemoveType => RemoveType(),
            WorldActions.TagGeneric => TagGeneric(),
            WorldActions.TagType => TagType(),
            WorldActions.DetachGeneric => DetachGeneric(),
            WorldActions.DetachType => DetachType(),
            WorldActions.Set => Set(),
            WorldActions.SubscribeWorldCreate => SubscribeWorldCreate(),
            WorldActions.SubscribeWorldDelete => SubscribeWorldDelete(),
            WorldActions.SubscribeAdd => SubscribeAdd(),
            WorldActions.SubscribeRemoved => SubscribeRemoved(),
            WorldActions.SubscribeAddGeneric => SubscribeAddGeneric(),
            WorldActions.SubscribeRemovedGeneric => SubscribeRemovedGeneric(),
            WorldActions.SubscribeWorldAdd => SubscribeWorldAdd(),
            WorldActions.SubscribeWorldRemoved => SubscribeWorldRemoved(),
            WorldActions.SubscribeTag => SubscribeTag(),
            WorldActions.SubscribeDetach => SubscribeDetach(),
            WorldActions.SubscribeWorldTag => SubscribeWorldTag(),
            WorldActions.SubscribeWorldDetach => SubscribeWorldDetach(),
            WorldActions.SubscribeDelete => SubscribeDelete(),
            _ => throw new ArgumentOutOfRangeException(nameof(thisAction), thisAction, null)
        });

        EnsureConsistency();

        _steps++;
    }


    private void EnsureConsistency()
    {
        _dead.All(e => !e.IsAlive)
            .Assert(this);

        _componentValues.All(kvp => 
                    !kvp.Key.IsNull &&
                    kvp.Key.IsAlive &&
                    kvp.Key.ComponentTypes.Length == kvp.Value.Count &&
                    kvp.Key.World == _worldState &&
                    kvp.Value.All(h => kvp.Key.Has(h.ComponentID)) &&
                    kvp.Value.All(h => kvp.Key.Get(h.ComponentID).Equals(h.RetrieveBoxed())))
                    .Assert(this);

        IEnumerable<Entity> allQueriedEntities = _worldState.CreateQuery()
            .Build()
            .EnumerateWithEntities()
            .AsEntityEnumerable();

        Assert(allQueriedEntities.Count() == _componentValues.Count);

        TestQuery2<C1, C2>();
        TestQuery2<C2, S1>();
        TestQuery2<S3, S2>();
        TestQuery2<S2, S2>();
        TestQuery2<C2, C2>();

        TestQuery2Exclude<C2, S1>();
        TestQuery2Exclude<S2, S1>();
        TestQuery2Exclude<S1, S1>();
        TestQuery2Exclude<C3, C3>();

        void TestQuery2<T1, T2>()
        {
            _worldState.Query<T1, T2>()
                .EnumerateWithEntities<T1, T2>()
                .AsEntityEnumerable()
                .Declare(out var enumerable)
                .Declare(l => l.Count(), out int count)
                .All(d =>
                    d.Entity.Get<T1>()!.Equals(d.C1) &&
                    d.Entity.Get<T2>()!.Equals(d.C2))
                .Assert(this);

            Assert(count == Entities.Count(e =>
                e.Has<T1>() &&
                e.Has<T2>()));

            Assert(enumerable.DistinctBy(t => t.Entity)
                .Count() == count);
        }

        void TestQuery2Exclude<T1, T2>()
        {
            _worldState
                .CreateQuery() // make a query builder
                .With<T1>() // include T1
                .Without<T2>() // exclude T2
                .Build() // get the actual query
                .EnumerateWithEntities<T1>() // get ref struct enumerator
                .AsEntityEnumerable() // evaluate into IEnumerable<T>
                .Declare(l => l.Count(), out int count) // save count for later
                .All(t => t.Entity.Get<T1>()!.Equals(t.C1)) // check if component values are correct
                .Assert(this); // throw otherwise

            Assert(count == Entities.Count(e =>
                e.Has<T1>() &&
                !e.Has<T2>()));
        }
    }

    public void Assert(bool pass, [CallerArgumentExpression(nameof(pass))] string? message = null)
    {
        if (!pass)
        {
            throw new InconsistencyException(message ?? "<unknown>", _steps, _seed);
        }
    }

    public void Dispose()
    {
        _worldState.Dispose();
    }
}