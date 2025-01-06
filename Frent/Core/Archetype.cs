using Frent.Buffers;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Core;

[Variadic("Archetype<T>", "Archetype<|T$, |>")]
[Variadic("typeof(T)", "|typeof(T$), |")]
[Variadic("Component<T>.ID", "|Component<T$>.ID, |")]
[Variadic("[Component<T>.CreateInstance()]", "[|Component<T$>.CreateInstance(), |]")]
internal class Archetype<T>
{
    public static readonly ImmutableArray<Type> ArchetypeTypes = new Type[] { typeof(T) }.ToImmutableArray();
    public static readonly ImmutableArray<ComponentID> ArchetypeComponentIDs = new ComponentID[] { Component<T>.ID }.ToImmutableArray();

    //ArchetypeTypes init first, then ID
    public static readonly EntityType ID = Archetype.GetArchetypeID(ArchetypeTypes.AsSpan(), [], ArchetypeTypes);
    public static readonly uint IDasUInt = (uint)ID.ID;

    //this method is literally only called once per world
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Archetype CreateNewArchetype(World world)
    {
        IComponentRunner[] runners = [Component<T>.CreateInstance()];
        var archetype = new Archetype(world, runners, Archetype.ArchetypeTable[ID.ID]);
        return archetype;
    }

    internal class OfComponent<C>
    {
        public static readonly int Index = GlobalWorldTables.ComponentIndex(ID, Component<C>.ID);
    }
}

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype(World world, IComponentRunner[] components, ArchetypeData archetypeData)
{
    internal EntityType ID => Data.ID;
    internal int MaxChunkSize => Data.MaxChunkSize;
    internal ImmutableArray<Type> ArchetypeTypeArray => Data.ComponentTypes;
    internal ImmutableArray<Type> ArchetypeTagArray => Data.TagTypes;

    internal readonly World World = world;
    internal readonly ArchetypeData Data = archetypeData;
    //the "raw" ID value
    internal readonly Dictionary<int, ArchetypeEdge> Graph = [];

    internal IComponentRunner[] Components = components;
    private Chunk<Entity>[] _entities = [new Chunk<Entity>(1)];

    private ushort _chunkIndex;
    private ushort _componentIndex;
    private int _chunkSize = 1;

    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Name))}";

    internal int EntityCount
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < _chunkIndex; i++)
            {
                sum += _entities[i].Length;
            }
            return sum + _componentIndex;
        }
    }

    internal ushort LastChunkComponentCount => _componentIndex;
    internal ushort ChunkCount => _chunkIndex;
    internal ushort CurrentWriteChunk => _chunkIndex;

    internal Span<Chunk<T>> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GlobalWorldTables.ComponentIndex(ID, Component<T>.ID);
        if (index > components.Length)
        {
            FrentExceptions.Throw_ComponentNotFoundException<T>();
            return default;
        }
        return ((IComponentRunner<T>)components[index]).AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Entity CreateEntityLocation(out EntityLocation entityLocation)
    {
        if (_entities[_chunkIndex].Length == _componentIndex)
            CreateChunks();

        entityLocation = new EntityLocation(ID, _chunkIndex, _componentIndex);
        return ref _entities[_chunkIndex][_componentIndex++];
    }

    private void CreateChunks()
    {
        _chunkIndex++;
        _componentIndex = 0;

        _chunkSize = Math.Min(MaxChunkSize, _chunkSize << 2);
        Chunk<Entity>.NextChunk(ref _entities, _chunkSize, _chunkIndex);
        foreach (var comprunner in Components)
            comprunner.AllocateNextChunk(_chunkSize, _chunkIndex);
    }

    public void EnsureCapacity(int size)
    {
        _chunkSize = (int)PreformanceHelpers.RoundDownToPowerOfTwo((uint)size);

        //TODO: make this better
        //don't really need it to be pow of two, just multiple of vector sizes

        while (size > 0)
        {
            Chunk<Entity>.NextChunk(ref _entities, _chunkSize, _chunkIndex);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk(_chunkSize, _chunkIndex);

            size -= _chunkSize;
        }
    }


    internal Entity DeleteEntity(ushort chunk, ushort comp)
    {
        if (unchecked(--_componentIndex == ushort.MaxValue))
        {
            _chunkIndex--;
            _componentIndex = (ushort)(_entities[_chunkIndex].Length - 1);
        }


        foreach (var comprunner in Components)
            comprunner.Delete(chunk, comp, _chunkIndex, _componentIndex);

        return _entities[chunk][comp] = _entities[_chunkIndex][_componentIndex];
    }

    internal void Update()
    {
        if (_chunkIndex == 0 && _componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(this);
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();
}