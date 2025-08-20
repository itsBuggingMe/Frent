namespace Frent.Fuzzing.Runner;

internal class WorldState : IDisposable
{
    private readonly Random _random;
    private readonly List<WorldActions> _actions = [];


    private readonly World _worldState;

    public WorldState(int seed)
    {
        _random = new Random(seed);
        _worldState = new();
    }

    public void Advance()
    {
        WorldActions thisAction = WorldActionsHelper.SelectWeightedAction(_random);
        _actions.Add(thisAction);

        switch (thisAction)
        {
            case WorldActions.CreateGeneric:

                break;
        }

        EnsureConsistency();
    }


    private void EnsureConsistency()
    {

    }

    public void Dispose()
    {
        _worldState.Dispose();
    }
}