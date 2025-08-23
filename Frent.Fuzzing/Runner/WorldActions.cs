using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace Frent.Fuzzing.Runner;

internal enum WorldActions
{
    [Weight(6)] CreateGeneric,
    [Weight(2)] CreateHandles,
    [Weight(2)] CreateObjects,

    [Weight(4)] Delete,

    [Weight(4)] AddGeneric,
    [Weight(1)] AddHandles,
    AddObject,
    AddAs,

    RemoveGeneric,
    RemoveType,

    TagGeneric,
    TagType,

    DetachGeneric,
    DetachType,

    Set,
}

internal static class WorldActionsHelper
{
    private static readonly ImmutableArray<(WorldActions Action, float Weight)> s_table =
        typeof(WorldActions)
            .GetMembers()
            .OfType<FieldInfo>()
            .Where(f => f.IsLiteral)
            .Select(m => ((Attribute.GetCustomAttribute(m, typeof(Weight)) as Weight)?.Value ?? 1, m))
            .Select(m => ((WorldActions)m.m.GetRawConstantValue()!, m.Item1))
            .ToImmutableArray();

    private static readonly float s_sum = s_table.Sum(t => t.Weight);

    [DebuggerStepThrough]
    public static WorldActions SelectWeightedAction(Random random)
    {
        float scaled = random.NextSingle() * s_sum;

        float cumulative = 0;
        for (int i = 0; i < s_table.Length; i++)
        {
            cumulative += s_table[i].Weight;
            if (cumulative > scaled)
            {
                return s_table[i].Action;
            }
        }

        throw new UnreachableException();
    }
}