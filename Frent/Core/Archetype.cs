using Frent.Buffers;
using Frent.Collections;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Frent.Core;

[Variadic("Archetype<T>", "Archetype<|T$, |>")]
[Variadic("[typeof(T)]", "[|typeof(T$), |]")]
[Variadic("[Component<T>.CreateInstance()]", "[|Component<T$>.CreateInstance(), |]")]
internal class Archetype<T>
{
    public static readonly Type[] ArchetypeTypes = [typeof(T)];
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
}

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
public class Archetype(int id, IComponentRunner[] components, World world, Type[] types)
{
    internal int ArchetypeID = id;
    internal readonly World World = world;
    internal readonly Type[] ArchetypeTypeArray = types;
    internal readonly Dictionary<int, ArchetypeEdge> Graph = [];

    internal IComponentRunner[] Components = components;
    private Chunk<Entity>[] _entities = [new Chunk<Entity>(1)];
    private ushort _chunkIndex;
    private ushort _componentIndex;

    internal string DebuggerDisplayString => $"Archetype: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Name))}";

    internal ushort LastChunkComponentCount => _componentIndex;
    internal ushort ChunkCount => _chunkIndex;
    internal ushort CurrentWriteChunk => _chunkIndex;

    internal Span<Chunk<T>> GetComponentSpan<T>() => ((IComponentRunner<T>)Components[GlobalWorldTables.ComponentLocationTable[ArchetypeID][Component<T>.ID]]).AsSpan();

    internal ref Entity CreateEntityLocation(out EntityLocation entityLocation)
    {
        if (_entities[_chunkIndex].Length == _componentIndex)
        {
            _chunkIndex++;
            _componentIndex = 0;

            Chunk<Entity>.NextChunk(ref _entities);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk();
        }

        entityLocation = new EntityLocation(this, _chunkIndex, _componentIndex);
        return ref _entities[_chunkIndex][_componentIndex++];
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
        foreach (var comprunner in Components)
            comprunner.Run(this);
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();

    #region Static Tables And Methods
    internal static FastStack<Type[]> ArchetypeTypes = FastStack<Type[]>.Create(16);
    internal static int NextArchetypeID = -1;
    private static readonly Dictionary<long, int> ExistingArchetypes = [];

    internal static Archetype CreateOrGetExistingArchetype(Type[] types, World world)
    {
        int id = GetArchetypeID(types.AsSpan(), types);
        ref Archetype archetype = ref world.GetArchetype((uint)id);
        if (archetype is not null)
            return archetype;

        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = Component.GetComponentRunnerFromType(types[i]);
        archetype = new Archetype(id, componentRunners, world, types);
        return archetype;
    }

    internal static int GetArchetypeID(Span<Type> types, Type[]? typesArray = null)
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
            var arr = typesArray ?? types.ToArray();
            ArchetypeTypes.Push(arr);
            ModifyComponentLocationTable(arr, finalID);
        }

        return finalID;
    }

    private static void ModifyComponentLocationTable(Type[] archetypeTypes, int id)
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

    private static long GetHash(Span<Type> types)
    {
        HashCode h1 = new();

        int i;
        for (i = 0; i < types.Length >> 1; i++)
            h1.Add(types[i]);

        HashCode h2 = new();
        for (; i < types.Length; i++)
            h2.Add(types[i]);

        return ((long)h1.ToHashCode() << 0x20) | (long)h2.ToHashCode();
    }
    #endregion
}