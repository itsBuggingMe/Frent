using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Core;
using Frent.Marshalling;

namespace Frent.Fuzzing.Runner;

internal partial class WorldState : IDisposable
{
    public static InconsistencyException? Fuzz(string[] args, bool captureException, out int seed)
    {
        if (args.Length != 2)
            throw new ArgumentException("Expecting two arguments corresponding to seed and step count.");
        if (!int.TryParse(args[0], out seed))
            throw new ArgumentException($"Seed value {args[0]} not an integer.");
        if (!int.TryParse(args[1], out int steps))
            throw new ArgumentException($"Step value {args[1]} not an integer.");

        int i = 0;
        try
        {
            using WorldState state = new WorldState(seed);

            for (; i < steps; i++)
            {
                state.Advance();
            }
        }
        catch (InconsistencyException e) when(captureException)
        {
            return e;
        }
        catch(Exception e) when (captureException)
        {
            return new InconsistencyException($"Unhandled Exception {e.GetType().Name}: {e.Message}", i, seed);
        }

        return null;
    }

    // record keeping
    private readonly Random _random;
    private readonly List<StepRecord> _actions = [];
    private readonly Dictionary<Entity, List<StepRecord>> _entityHistory = [];
    private int _steps;
    private int _seed;

    // actual state
    private readonly World _worldState;

    // expected state
    private Dictionary<Entity, List<ComponentHandle>> _componentValues = [];
    private Dictionary<Entity, List<TagID>> _tagValues = [];

    private IEnumerable<Entity> Entities => _componentValues.Select(kvp => kvp.Key);
    private HashSet<Entity> _dead = [];

    private EventRecord[] _expectedEventTable;
    private int[] _expectedEventSubscriptions;

    private EventRecord[] _actualEventTable;

    public WorldState(int seed)
    {
        _seed = seed;
        _random = new Random(seed);
        _worldState = new();

        _expectedEventTable = new EventRecord[13];
        _expectedEventSubscriptions = new int[13];
        _actualEventTable = new EventRecord[13];

        Component.RegisterComponent<object>();
    }

    public void Advance()
    {
        WorldActions thisAction = WorldActionsHelper.SelectWeightedAction(_random);

        StepRecord stepTaken = thisAction switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(thisAction), thisAction, null)
        };

        stepTaken = stepTaken with
        {
            Action = thisAction,
            Step = _steps,
        };

        _actions.Add(stepTaken);
        stepTaken.Playback?.Invoke();

        if (!stepTaken.Entity.IsNull)
        {
            (CollectionsMarshal.GetValueRefOrAddDefault(_entityHistory, stepTaken.Entity, out _) ??= [])
                .Add(stepTaken);
        }

        EnsureConsistency();

        _steps++;
    }


    private void EnsureConsistency()
    {
        _dead.All(e => !e.IsAlive)
            .Assert(this);

        foreach (var kvp in _componentValues)
        {
            (!kvp.Key.IsNull).Assert(this);
            (kvp.Key.IsAlive).Assert(this);
            (kvp.Key.ComponentTypes.Length == kvp.Value.Count).Assert(this);
            (kvp.Key.World == _worldState).Assert(this);
            (kvp.Value.All(h => kvp.Key.Has(h.ComponentID))).Assert(this);
            foreach (var h in kvp.Value)
            {
                kvp.Key.Get(h.ComponentID).Equals(h.RetrieveBoxed()).Assert(this);
            }
        }

        foreach(var kvp in _tagValues)
        {
            kvp.Value.All(kvp.Key.Tagged).Assert(this);
        }

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

        void TestQuery2<T1, T2>()
        {
            _worldState.Query<T1, T2>()
                .EnumerateWithEntities<T1, T2>()
                .AsEntityEnumerable()
                .Declare(out var enumerable)
                .All(d =>
                    d.Entity.Get<T1>()!.Equals(d.C1) &&
                    d.Entity.Get<T2>()!.Equals(d.C2))
                .Assert(this);

            Assert(enumerable.Count() == Entities.Where(e =>
                e.Has<T1>() &&
                e.Has<T2>())
                .Declare(out var x)
                .Count());

            Assert(enumerable.DistinctBy(t => t.Entity)
                .Count() == enumerable.Count());
        }

        TestQuery2Exclude<C2, C3>();
        TestQuery2Exclude<C2, S1>();
        TestQuery2Exclude<S2, S1>();

        TestQuery2Exclude<S1, S1>();
        TestQuery2Exclude<C3, C3>();

        void TestQuery2Exclude<T1, T2>()
        {
            _worldState
                .CreateQuery() // make a query builder
                .With<T1>() // include T1
                .Without<T2>() // exclude T2
                .Build() // get the actual query
                .EnumerateWithEntities<T1>() // get ref struct enumerator
                .AsEntityEnumerable() // evaluate into IEnumerable<T>
                .Declare(out var enumerable) // save count for later
                .All(t => t.Entity.Get<T1>()!.Equals(t.C1)) // check if component values are correct
                .Assert(this); // throw otherwise

            Assert(enumerable.Count() == Entities.Where(e =>
                e.Has<T1>() &&
                !e.Has<T2>())
                .Declare(out var x)
                .Count());
        }
    }

    [DebuggerHidden]
    public void Assert(bool pass, [CallerArgumentExpression(nameof(pass))] string? message = null)
    {
        if (!pass)
        {
            throw new InconsistencyException(message ?? "<unknown>", _steps, _seed);
        }
    }

    public void Subscribe(Entity? entity, WorldActions eventKind)
    {
        ArgumentOutOfRangeException.ThrowIfNegative((int)eventKind);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)eventKind, 12);
    }

    public void Dispose()
    {
        _worldState.Dispose();
    }
}