using Frent.Buffers;
using Frent.Collections;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Frent.Core;

[Variadic("Archetype<T>", "Archetype<|T$, |>")]
[Variadic("typeof(T)", "|typeof(T$), |")]
[Variadic("[Component<T>.CreateInstance()]", "[|Component<T$>.CreateInstance(), |]")]
internal class Archetype<T>
{
    public static readonly ImmutableArray<Type> ArchetypeTypes = new Type[] { typeof(T) }.ToImmutableArray();
    //ArchetypeTypes init first, then ID
    public static readonly int ID = Archetype.GetArchetypeID(ArchetypeTypes.AsSpan(), ArchetypeTypes);
    public static readonly uint IDasUInt = (uint)ID;

    public static Archetype CreateArchetype(World world)
    {
        ref Archetype archetype = ref world.GetArchetype(IDasUInt);
        if (archetype is not null)
            return archetype;
        IComponentRunner[] runners = [Component<T>.CreateInstance()];
        archetype = new Archetype(ID, runners, world, ArchetypeTypes);
        return archetype;
    }

    internal class OfComponent<C>
    {
        public static readonly int Index = GlobalWorldTables.ComponentLocationTable[ID][Component<C>.ID];
    }
}

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
public class Archetype(int id, IComponentRunner[] components, World world, ImmutableArray<Type> types)
{
    private const int MaxChunkSize = 8192;

    internal int ArchetypeID = id;
    internal readonly World World = world;
    internal readonly ImmutableArray<Type> ArchetypeTypeArray = types;
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
            for(int i = 0; i < _chunkIndex; i++)
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
        byte index = GlobalWorldTables.ComponentLocationTable[ArchetypeID][Component<T>.ID];
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

        while(size > 0)
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
        if(_chunkIndex == 0 && _componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(this);
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();

    #region Static Tables And Methods
    internal static FastStack<ImmutableArray<Type>> ArchetypeTypes = FastStack<ImmutableArray<Type>>.Create(16);
    internal static int NextArchetypeID = -1;
    private static readonly Dictionary<long, int> ExistingArchetypes = [];

    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<Type> types, World world, ImmutableArray<Type>? typeArray = null)
    {
        int id = GetArchetypeID(types, typeArray);
        ref Archetype archetype = ref world.GetArchetype((uint)id);
        if (archetype is not null)
            return archetype;

        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = Component.GetComponentRunnerFromType(types[i]);
        archetype = new Archetype(id, componentRunners, world, typeArray ?? ReadOnlySpanToImmutableArray(types));
        world.ArchetypeAdded(archetype);
        return archetype;
    }

    internal static int GetArchetypeID(ReadOnlySpan<Type> types, ImmutableArray<Type>? typesArray = null)
    {
        ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(ExistingArchetypes, GetHash(types), out bool exists);
        int finalID;

        if (exists)
        {
            finalID = slot;
        }
        else
        {
            slot = finalID = Interlocked.Increment(ref NextArchetypeID);
            var arr = typesArray ?? ReadOnlySpanToImmutableArray(types);
            ArchetypeTypes.Push(arr);
            ModifyComponentLocationTable(arr, finalID);
        }

        return finalID;
    }

    private static void ModifyComponentLocationTable(ImmutableArray<Type> archetypeTypes, int id)
    {
        if (GlobalWorldTables.ComponentLocationTable.Length == id)
        {
            Array.Resize(ref GlobalWorldTables.ComponentLocationTable, Math.Max(id << 1, 1));
        }

        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            _ = Component.GetComponentID(archetypeTypes[i]);
        }

        ref var componentTable = ref GlobalWorldTables.ComponentLocationTable[id];
        componentTable = new byte[Component.ComponentTableBufferSize];
        componentTable.AsSpan().Fill(byte.MaxValue);
        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            componentTable[Component.GetComponentID(archetypeTypes[i])] = (byte)i;
        }
    }

    private static long GetHash(ReadOnlySpan<Type> types)
    {
        HashCode h1 = new();

        int i;
        for (i = 0; i < types.Length >> 1; i++)
        {
            h1.Add(types[i]);
        }

        HashCode h2 = new();
        for (; i < types.Length; i++)
        {
            h2.Add(types[i]);
        }

        var hash = ((long)h1.ToHashCode() * 1610612741) + h2.ToHashCode();

        return hash;
    }

    private static ImmutableArray<Type> ReadOnlySpanToImmutableArray(ReadOnlySpan<Type> types)
    {
        var builder = ImmutableArray.CreateBuilder<Type>(types.Length);
        for(int i = 0; i < types.Length; i++)
            builder.Add(types[i]);
        return builder.MoveToImmutable();
    }
    #endregion
}