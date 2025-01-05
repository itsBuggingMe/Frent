using Frent.Buffers;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;

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
    public static readonly EntityType ID = Archetype.GetArchetypeID(ArchetypeTypes.AsSpan(), ArchetypeTypes);
    public static readonly uint IDasUInt = (uint)ID.ID;

    internal static Archetype CreateArchetype(World world)
    {
        ref Archetype archetype = ref world.GetArchetype(IDasUInt);
        if (archetype is not null)
            return archetype;
        IComponentRunner[] runners = [Component<T>.CreateInstance()];
        archetype = new Archetype(world, runners, Archetype.ArchetypeTable[ID.ID]);
        return archetype;
    }

    internal class OfComponent<C>
    {
        public static readonly int Index = GlobalWorldTables.ComponentLocationTable[ID.ID][Component<C>.ID.ID];
    }
}

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype(World world, IComponentRunner[] components, ArchetypeData archetypeData)
{
    internal EntityType ID => Data.ID;
    internal int MaxChunkSize => Data.MaxChunkSize;
    internal ImmutableArray<Type> ArchetypeTypeArray => Data.ComponentTypes;

    internal readonly World World = world;
    internal readonly ArchetypeData Data = archetypeData;
    internal readonly Dictionary<ComponentID, ArchetypeEdge> Graph = [];

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
        byte index = GlobalWorldTables.ComponentLocationTable[ID.ID][Component<T>.ID.ID];
        if (index > components.Length)
        {
            FrentExceptions.Throw_ComponentNotFoundException<T>();
            return default;
        }
        return ((IComponentRunner<T>)components[index]).AsSpan();
    }

    internal ref Entity CreateEntityLocation(out EntityLocation entityLocation)
    {
        if (_entities[_chunkIndex].Length == _componentIndex)
        {
            _chunkIndex++;
            _componentIndex = 0;

            _chunkSize = Math.Min(MaxChunkSize, _chunkSize << 1);
            Chunk<Entity>.NextChunk(ref _entities, _chunkSize);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk(_chunkSize);
        }

        entityLocation = new EntityLocation(this, _chunkIndex, _componentIndex);
        return ref _entities[_chunkIndex][_componentIndex++];
    }

    public void EnsureCapacity(int size)
    {
        _chunkSize = (int)Math.Min(MaxChunkSize, BitOperations.RoundUpToPowerOf2((uint)(size >> 1)));//round down to power of two

        while (size > 0)
        {
            Chunk<Entity>.NextChunk(ref _entities, _chunkSize);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk(_chunkSize);

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