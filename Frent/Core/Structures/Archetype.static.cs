using Frent.Collections;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

[Variadic("Archetype<T>", "Archetype<|T$, |>")]
[Variadic("typeof(T)", "|typeof(T$), |")]
[Variadic("Component<T>.ID", "|Component<T$>.ID, |")]
[Variadic("[Component<T>.CreateInstance()]", "[|Component<T$>.CreateInstance(), |]")]
internal static class Archetype<T>
{
    public static readonly ImmutableArray<ComponentID> ArchetypeComponentIDs = new ComponentID[] { Component<T>.ID }.ToImmutableArray();

    //ArchetypeTypes init first, then ID
    public static readonly ArchetypeID ID = Archetype.GetArchetypeID(ArchetypeComponentIDs.AsSpan(), [], ArchetypeComponentIDs, ImmutableArray<TagID>.Empty);
    public static readonly uint IDasUInt = ID.ID;

    internal static Archetype CreateNewOrGetExistingArchetype(World world)
    {
        var index = IDasUInt;
        ref Archetype archetype = ref world.WorldArchetypeTable.UnsafeArrayIndex(index);
        if (archetype is null)
            CreateArchetype(out archetype, world);
        return archetype!;

        //this method is literally only called once per world
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CreateArchetype(out Archetype archetype, World world)
        {
            IComponentRunner[] runners = [Component<T>.CreateInstance()];
            archetype = new Archetype(ID, runners);
            world.ArchetypeAdded(archetype.ID);
        }
    }

    internal static class OfComponent<C>
    {
        public static int Index = GlobalWorldTables.ComponentIndex(ID, Component<C>.ID);
    }
}

partial class Archetype
{
    internal static readonly ArchetypeID Default = default(ArchetypeID);
    internal static FastStack<ArchetypeData> ArchetypeTable = FastStack<ArchetypeData>.Create(16);
    internal static int NextArchetypeID = -1;

    private static readonly Dictionary<long, ArchetypeData> ExistingArchetypes = [];

    //TODO: make this use component Ids instead
    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> tagTypes, World world, ImmutableArray<ComponentID>? typeArray = null, ImmutableArray<TagID>? tagTypesArray = null)
    {
        ArchetypeID id = GetArchetypeID(types, tagTypes, typeArray, tagTypesArray);
        ref Archetype archetype = ref world.WorldArchetypeTable[id.ID];
        if (archetype is not null)
            return archetype;

        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = Component.GetComponentRunnerFromType(types[i].Type);

        archetype = new Archetype(id, componentRunners);
        world.ArchetypeAdded(archetype.ID);

        return archetype;
    }

    //initalize default(ArchetypeID) to point to empty archetype
    static Archetype() => GetArchetypeID([], []);

    internal static ArchetypeID GetArchetypeID(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> tagTypes, ImmutableArray<ComponentID>? typesArray = null, ImmutableArray<TagID>? tagTypesArray = null)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            ref ArchetypeData slot = ref CollectionsMarshal.GetValueRefOrAddDefault(ExistingArchetypes, GetHash(types, tagTypes), out bool exists);
            ArchetypeID finalID;

            if (exists)
            {
                //can't be null if entry exists
                finalID = slot!.ID;
            }
            else
            {
                int nextIDInt = Interlocked.Increment(ref NextArchetypeID);
                if (nextIDInt == ushort.MaxValue)
                    throw new InvalidOperationException($"Exceeded maximum unique archetype count of 65535");
                finalID = new ArchetypeID((ushort)nextIDInt);

                var arr = typesArray ?? MemoryHelpers.ReadOnlySpanToImmutableArray(types);
                var tagArr = tagTypesArray ?? MemoryHelpers.ReadOnlySpanToImmutableArray(tagTypes);

                slot = CreateArchetypeData(finalID, arr, tagArr);
                ArchetypeTable.Push(slot);
                ModifyComponentLocationTable(arr, tagArr, finalID.ID);
            }

            return finalID;
        }
    }

    public static ArchetypeData CreateArchetypeData(ArchetypeID id, ImmutableArray<ComponentID> componentTypes, ImmutableArray<TagID> tagTypes)
    {
        return new ArchetypeData(id, componentTypes, tagTypes);
    }

    private static void ModifyComponentLocationTable(ImmutableArray<ComponentID> archetypeTypes, ImmutableArray<TagID> archetypeTags, int id)
    {
        if (GlobalWorldTables.ComponentTagLocationTable.Length == id)
        {
            int size = Math.Max(id << 1, 1);
            Array.Resize(ref GlobalWorldTables.ComponentTagLocationTable, size);
            foreach (var world in GlobalWorldTables.Worlds.AsSpan())
            {
                if (world is World w)
                {
                    w.UpdateArchetypeTable(size);
                }
            }
        }

        //for (int i = 0; i < archetypeTypes.Length; i++)
        //{
        //    _ = Component.GetComponentID(archetypeTypes[i].Type);
        //}

        ref var componentTable = ref GlobalWorldTables.ComponentTagLocationTable[id];
        componentTable = new byte[GlobalWorldTables.ComponentTagTableBufferSize];
        componentTable.AsSpan().Fill(GlobalWorldTables.DefaultNoTag);

        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            componentTable[archetypeTypes[i].ID] = (byte)i;
        }

        for (int i = 0; i < archetypeTags.Length; i++)
        {
            componentTable[archetypeTags[i].ID] |= GlobalWorldTables.HasTagMask;
        }
    }

    private static long GetHash(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> andMoreTypes)
    {
        HashCode h1 = new();
        HashCode h2 = new();

        int i;
        for (i = 0; i < types.Length >> 1; i++)
        {
            h1.Add(types[i]);
        }


        var hash1 = 0U;
        var hash2 = 0U;

        foreach (var item in andMoreTypes)
        {
            hash1 ^= item.ID * 98317U;
            hash2 += item.ID * 53U;
        }

        h1.Add(HashCode.Combine(hash1, hash2));

        for (; i < types.Length; i++)
        {
            h2.Add(types[i]);
        }

        var hash = ((long)h1.ToHashCode() * 1610612741) + h2.ToHashCode();

        return hash;
    }
}
