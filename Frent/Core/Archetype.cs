using Frent.Updating;
using Frent.Collections;
using Frent.Components;
using System.Runtime.InteropServices;
using Frent.Buffers;

namespace Frent.Core;

internal class Archetype<T>
{
    public static readonly int ID = Archetype.GetArchetypeID([typeof(T)]);
    public static readonly Type[] ArchetypeTypes = [typeof(T)];
    public static Table<Archetype> ByWorld = new Table<Archetype>(2);

    public static Archetype CreateArchetype(World world)
    {
        IComponentRunner[] runners = [UpdateHelper<T>.CreateInstance()];
        return new Archetype(ID, runners, world);
    }
}

internal class Archetype(int id, IComponentRunner[] components, World world)
{
    internal int ArchetypeID = id;
    internal Chunk<Entity>[] Entities = [new Chunk<Entity>(1)];
    internal IComponentRunner[] Components = components;
    internal readonly World World = world;

    private ushort _chunkIndex;
    private ushort _componentIndex;

    internal Span<Chunk<T>> GetComponentSpan<T>() where T : IComponent => ((IComponentRunner<T>)Components[GlobalWorldTables.ComponentLocationTable[ArchetypeID][Component<T>.ID]]).AsSpan();

    internal void CreateEntityLocation(out EntityLocation entityLocation)
    {
        entityLocation = new EntityLocation(this, _chunkIndex, _componentIndex);
        throw new NotImplementedException();
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => Entities.AsSpan();

    #region Static Tables And Methods
    internal static FastStack<Type[]> ArchetypeTypes = FastStack<Type[]>.Create(16);
    internal static int NextArchetypeID = -1;
    private static readonly Dictionary<long, int> ExistingArchetypes = [];

    public static Archetype CreateArchetype(Type[] types, World world)
    {
        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = UpdateHelper.GetComponentRunnerFromType(types[i]);
        return new Archetype(GetArchetypeID(types.AsSpan(), types), componentRunners, world);
    }

    public static int GetArchetypeID(Span<Type> types, Type[]? typesArray = null)
    {
        ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(ExistingArchetypes, GetHash(types), out bool exists);
        int finalID;

        if(exists)
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
        if(GlobalWorldTables.ComponentLocationTable.Length == id)
        {
            Array.Resize(ref GlobalWorldTables.ComponentLocationTable, id << 1);
        }

        Span<Type> columnTypes = Component.AllComponentTypesOrdered.AsSpan();
        var allComponents = GlobalWorldTables.ComponentLocationTable[id];

        for (int i = 0; i < columnTypes.Length; i++)
        {
            int index = Array.IndexOf(archetypeTypes, columnTypes[i]);
            allComponents[i] = index == -1 ? byte.MaxValue : (byte)index;
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