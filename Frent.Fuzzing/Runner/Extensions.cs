using Frent.Systems;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Security.AccessControl;

namespace Frent.Fuzzing.Runner;

internal static class Extensions
{
    [DebuggerHidden]
    public static void Assert(this bool pass, WorldState state, [CallerArgumentExpression(nameof(pass))] string? message = null)
    {
        state.Assert(pass, message);
    }

    public static IEnumerable<Entity> AsEntityEnumerable(this EntityQueryEnumerator.Enumerable enumerator)
    {
        List<Entity> items = [];
        foreach (var entity in enumerator)
            items.Add(entity);
        return items;
    }

    public static IEnumerable<(Entity Entity, T C1)> AsEntityEnumerable<T>(this EntityQueryEnumerator<T>.Enumerable enumerator)
    {
        List<(Entity, T)> items = [];
        foreach (var tup in enumerator)
            items.Add((tup.Entity, tup.Item1.Value));
        return items;
    }

    public static IEnumerable<(Entity Entity, T1 C1, T2 C2)> AsEntityEnumerable<T1, T2>(this EntityQueryEnumerator<T1, T2>.Enumerable enumerator)
    {
        List<(Entity, T1, T2)> items = [];
        foreach (var tup in enumerator)
            items.Add((tup.Entity, tup.Item1.Value, tup.Item2.Value));
        return items;
    }

    public static IEnumerable<T> AsEnumerable<T>(this QueryEnumerator<T>.Enumerable enumerator)
    {
        List<T> items = [];
        foreach (var tup in enumerator)
            items.Add(tup.Item1.Value);
        return items;
    }

    public static IEnumerable<(T1 C1, T2 C2)> AsEnumerable<T1, T2>(this QueryEnumerator<T1, T2>.Enumerable enumerator)
    {
        List<(T1, T2)> items = [];
        foreach (var tup in enumerator)
            items.Add((tup.Item1.Value, tup.Item2.Value));
        return items;
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> values, Random random)
    {
        var result = values.ToArray();
        random.Shuffle(result);
        return result;
    }

    public static T Declare<T, TResult>(this T item, Func<T, TResult> selector, out TResult variable)
    {
        variable = selector(item);
        return item;
    }

    public static T Declare<T>(this T item, out T variable)
    {
        variable = item;
        return item;
    }
}
