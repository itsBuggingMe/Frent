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
    public static readonly ArchetypeID ID = Archetype.GetArchetypeID(ArchetypeTypes.AsSpan(), [], ArchetypeTypes);
    public static readonly uint IDasUInt = (uint)ID.ID;

    //this method is literally only called once per world
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Archetype CreateNewArchetype(World world)
    {
        IComponentRunner[] runners = [Component<T>.CreateInstance()];
        var archetype = new Archetype(world, runners, Archetype.ArchetypeTable[ID.ID]);
        world.ArchetypeAdded(archetype);
        return archetype;
    }

    internal class OfComponent<C>
    {
        public static int Index = GlobalWorldTables.ComponentIndex(ID, Component<C>.ID);
    }
}

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype(World world, IComponentRunner[] components, ArchetypeData archetypeData)
{
    internal ArchetypeID ID => Data.ID;
    internal int MaxChunkSize => Data.MaxChunkSize;
    internal ImmutableArray<Type> ArchetypeTypeArray => Data.ComponentTypes;
    internal ImmutableArray<Type> ArchetypeTagArray => Data.TagTypes;

    //48 bytes
    //thats chonky
    internal readonly World World = world;
    internal readonly ArchetypeData Data = archetypeData;
    //the "raw" ID value
    //im ok with using bcl dictionary b/c inital capacity is zero
    internal readonly Dictionary<int, ArchetypeEdge> Graph = [];

    internal IComponentRunner[] Components = components;
    private Chunk<Entity>[] _entities = [new Chunk<Entity>(1)];

    private ushort _chunkIndex;
    private int _componentIndex;
    private int _chunkSize = 1;

    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Name))} Tags: {string.Join(", ", ArchetypeTagArray.Select(t => t.Name))}";

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

    internal int LastChunkComponentCount => _componentIndex;
    internal ushort ChunkCount => _chunkIndex;
    internal ushort CurrentWriteChunk => _chunkIndex;

    internal Span<Chunk<T>> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GlobalWorldTables.ComponentIndex(ID, Component<T>.ID);
        if (index > components.Length)
        {
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
            return default;
        }
        return ((IComponentRunner<T>)components[index]).AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Entity CreateEntityLocation(out EntityLocation entityLocation)
    {
        if (_entities.UnsafeArrayIndex(_chunkIndex).Length == _componentIndex)
            CreateChunks();

        entityLocation = new EntityLocation(ID, _chunkIndex, (ushort)_componentIndex);
        return ref _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex++];
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
        _chunkSize = MaxChunkSize;

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
        if (unchecked(--_componentIndex == -1))
        {
            _chunkIndex--;
            _componentIndex = _entities[_chunkIndex].Length - 1;

            foreach (var comprunner in Components)
                comprunner.Delete(chunk, comp, _chunkIndex, (ushort)_componentIndex);

            var e =  _entities.UnsafeArrayIndex(chunk)[comp] = _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex];

            int index = _chunkIndex + 1;
            _entities[index].Return();
            foreach (var comprunner in Components)
                comprunner.Trim(index);

            return e;
        }


        foreach (var comprunner in Components)
            comprunner.Delete(chunk, comp, _chunkIndex, (ushort)_componentIndex);

        return _entities.UnsafeArrayIndex(chunk)[comp] = _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex];
    }

    internal void Update()
    {
        if (_chunkIndex == 0 && _componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(this);
    }

    internal void Update(ComponentID componentID)
    {
        if (_chunkIndex == 0 && _componentIndex == 0)
            return;
        
        int compIndex = GlobalWorldTables.ComponentIndex(Data.ID, componentID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            return;
        
        Components[componentID.ID].Run(this);
    }

    internal void MultiThreadedUpdate(Config config)
    {
        throw new NotImplementedException();
    }

    internal void ReleaseArrays()
    {
        for(int i = 0; i <= _chunkIndex; i++)
        {
            _entities[i].Return();
            foreach (var comprunner in Components)
                comprunner.Trim(i);
        }
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();
}