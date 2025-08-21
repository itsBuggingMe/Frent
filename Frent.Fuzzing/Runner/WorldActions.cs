using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace Frent.Fuzzing.Runner;

internal enum WorldActions
{
    [Weight(0.1f)] SubscribeWorldCreate = 0,
    [Weight(0.1f)] SubscribeWorldDelete = 1,

    [Weight(0.25f)] SubscribeAdd = 2,
    [Weight(0.25f)] SubscribeRemoved = 3,

    [Weight(0.25f)] SubscribeAddGeneric = 4,
    [Weight(0.25f)] SubscribeRemovedGeneric = 5,

    [Weight(0.1f)] SubscribeWorldAdd = 6,
    [Weight(0.1f)] SubscribeWorldRemoved = 7,

    [Weight(0.25f)] SubscribeTag = 8,
    [Weight(0.25f)] SubscribeDetach = 9,

    [Weight(0.1f)] SubscribeWorldTag = 10,
    [Weight(0.1f)] SubscribeWorldDetach = 11,

    [Weight(0.25f)] SubscribeDelete = 12,

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