using System.Collections.Immutable;

namespace Frent.Fuzzing.Runner;

internal static class Assert
{
    public static void Fuzz(string[] args)
    {
        if (args.Length != 2)
            throw new ArgumentException("Expecting two arguments corresponding to seed and step count.");
        if (!int.TryParse(args[0], out int seed))
            throw new ArgumentException($"Seed value {args[0]} not an integer.");
        if (!int.TryParse(args[1], out int steps))
            throw new ArgumentException($"Seed value {args[0]} not an integer.");

        using WorldState state = new WorldState(seed);

        for (int i = 0; i < steps; i++)
        {
            state.Advance();
        }
    }
}
