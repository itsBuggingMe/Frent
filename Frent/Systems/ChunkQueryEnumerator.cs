using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("                Item1 = cur.GetComponentSpan<T>(),", 
    "|                Item$ = cur.GetComponentSpan<T$>(),\n|")]
[Variadic("<T>", "<|T$, |>")]
public ref struct ChunkQueryEnumerator<T>
{
    //ptr, ptr, int, int is better alignment
    private World _world;
    private Span<Archetype> _archetypes; 
    private int _archetypeIndex;
    private ChunkQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
    }

    public ChunkTuple<T> Current
    {
        get
        {
            Archetype cur = _archetypes[_archetypeIndex];
            return new()
            {
                Item1 = cur.GetComponentSpan<T>(),
            };
        }
    }
    public void Dispose()
    {
        _world.ExitDisallowState();
    }

    public bool MoveNext() => ++_archetypeIndex < _archetypes.Length;

    public struct QueryEnumerable(Query query)
    {
        public ChunkQueryEnumerator<T> GetEnumerator() => new(query);
    }
}