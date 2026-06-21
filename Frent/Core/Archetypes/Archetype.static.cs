using Frent.Collections;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Frent.Core.Archetypes;

[Variadic(nameof(Archetype))]
internal static class Archetype<T>
{
    public static readonly ImmutableArray<ComponentID> ArchetypeComponentIDs = CreateComponentIDArray();

    [SkipLocalsInit]
    private static ImmutableArray<ComponentID> CreateComponentIDArray()
    {
        Span<ComponentID> ids = stackalloc ComponentID[1];
        int index = 0;

        if (!Component<T>.IsSparseComponent) ids.UnsafeSpanIndex(index++) = Component<T>.ID;

        return ImmutableArray.Create(ids.Slice(0, index));
    }

    //ArchetypeTypes init first, then ID
    public static readonly ArchetypeID ID = Archetype.GetArchetypeID(ArchetypeComponentIDs.AsSpan(), [], ArchetypeComponentIDs, ImmutableArray<TagID>.Empty);

    internal static World.WorldArchetypeTableItem CreateNewOrGetExistingArchetypes(World world)
    {
        var index = ID.RawIndex;
        ref World.WorldArchetypeTableItem archetypes = ref world.WorldArchetypeTable.UnsafeArrayIndex(index);
        if (archetypes.Archetype is null)
            archetypes = CreateArchetypes(world);
        return archetypes;

        //this method is literally only called once per world
        [MethodImpl(MethodImplOptions.NoInlining)]
        static World.WorldArchetypeTableItem CreateArchetypes(World world)
        {
            VariadicHelpers.CreateArchetypeBuffers(ArchetypeComponentIDs, ID, 
                out var runners,
                out var tmpStorages,
                out var map);

            Component<T>.InitalizeComponentRunnerImpl(runners, tmpStorages, map);

            return VariadicHelpers.CreateArchetype(ID, world, runners, tmpStorages);
        }

    }

    internal static class OfComponent<C>
    {
        public static readonly int Index = GlobalWorldTables.ComponentIndex(ID, Component<C>.ID);
    }
}

partial class Archetype
{
    internal static readonly ArchetypeID Null;
    internal static FastStack<ArchetypeData> ArchetypeTable = FastStack<ArchetypeData>.Create(16);
    internal static int NextArchetypeID = -1;

    private static readonly RefDictionary<ulong, ArchetypeData> ExistingArchetypes = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyBitset(Archetype from, Archetype to, int fromIndex, int toIndex)
    {
        var sparseBits = from.BitsetArray;

        to.GetBitset(toIndex) = (uint)fromIndex < (uint)sparseBits.Length ?
            sparseBits[fromIndex] :
            default;// implicit default(Bitset)
    }

    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> tagTypes, World world, ImmutableArray<ComponentID>? typeArray = null, ImmutableArray<TagID>? tagTypesArray = null)
    {
        ArchetypeID id = GetArchetypeID(types, tagTypes, typeArray, tagTypesArray);
        return CreateOrGetExistingArchetype(id, world).Archetype;
    }

    internal static World.WorldArchetypeTableItem CreateOrGetExistingArchetypes(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> tagTypes, World world, ImmutableArray<ComponentID>? typeArray = null, ImmutableArray<TagID>? tagTypesArray = null)
    {
        ArchetypeID id = GetArchetypeID(types, tagTypes, typeArray, tagTypesArray);
        return CreateOrGetExistingArchetype(id, world);
    }

    internal static World.WorldArchetypeTableItem CreateOrGetExistingArchetype(ArchetypeID id, World world)
    {
        ref World.WorldArchetypeTableItem archetype = ref world.WorldArchetypeTable[id.RawIndex];
        if (archetype.Archetype is not null)
            return archetype;

        var types = id.Types;
        ComponentStorageRecord[] componentRunners = new ComponentStorageRecord[types.Length + 1];
        ComponentStorageRecord[] tmpRunners = new ComponentStorageRecord[types.Length + 1];
        for (int i = 1; i < componentRunners.Length; i++)
        {
            var fact = Component.GetComponentFactoryFromType(types[i - 1].Type);
            componentRunners[i] = fact.Create(1);
            tmpRunners[i] = fact.Create(0);
        }

        Archetype normal = new Archetype(id, componentRunners, false);
        Archetype tmpCreateArchetype = new Archetype(id, tmpRunners, true);

        archetype = new World.WorldArchetypeTableItem(normal, tmpCreateArchetype);
        world.ArchetypeAdded(normal, tmpCreateArchetype);

        return archetype;
    }

    internal static Archetype GetAdjacentArchetypeLookup(World world, ArchetypeEdgeKey edge)
    {
        if (world.ArchetypeGraphEdges.TryGetValue(edge, out var archetype))
            return archetype;
        return GetAdjacentArchetypeCold(world, edge);
    }

    internal static Archetype GetAdjacentArchetypeCold(World world, ArchetypeEdgeKey edge)
    {
        //this world doesn't have the archetype, or it doesnt even exist

        Archetype from = edge.ArchetypeFrom.Archetype(world)!;
        ImmutableArray<ComponentID> fromComponents = edge.ArchetypeFrom.Types;
        ImmutableArray<TagID> fromTags = edge.ArchetypeFrom.Tags;

        switch (edge.EdgeType)
        {
            case ArchetypeEdgeType.AddComponent:
                fromComponents = MemoryHelpers.Concat(fromComponents, edge.ComponentID);
                break;
            case ArchetypeEdgeType.RemoveComponent:
                fromComponents = MemoryHelpers.Remove(fromComponents, edge.ComponentID);
                break;
            case ArchetypeEdgeType.AddTag:
                fromTags = MemoryHelpers.Concat(fromTags, edge.TagID);
                break;
            case ArchetypeEdgeType.RemoveTag:
                fromTags = MemoryHelpers.Remove(fromTags, edge.TagID);
                break;
        }

        var archetype = CreateOrGetExistingArchetype(fromComponents.AsSpan(), fromTags.AsSpan(), world, fromComponents, fromTags);

        return archetype;
    }

    static Archetype()
    {
        Null = GetArchetypeID([Component.GetComponentID(typeof(void))], [Tag.GetTagID(typeof(Disable))]);

        //Deferred creation entities fully supported
        ////this archetype exists only so that "EntityLocation"s of deferred archetypes have something to point to
        ////disable so less overhead
        //DeferredCreate = GetArchetypeID([], [Tag.GetTagID(typeof(DeferredCreate)), Tag.GetTagID(typeof(Disable))]);
    }

    internal static ArchetypeID GetArchetypeID(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> tagTypes, ImmutableArray<ComponentID>? typesArray = null, ImmutableArray<TagID>? tagTypesArray = null)
    {
        if (types.Length > MemoryHelpers.MaxComponentCount)
            throw new InvalidOperationException("Entities can have a max of 127 components!");

        lock (GlobalWorldTables.BufferChangeLock)
        {
            if (MemoryHelpers.HasDuplicateIDs(types, out ComponentID compDupe))
                throw new InvalidOperationException($"Attempted to create entity with duplicate components: {compDupe.Type.Name}");
            if (MemoryHelpers.HasDuplicateIDs(tagTypes, out TagID tagDupe))
                throw new InvalidOperationException($"Attempted to create entity with duplicate tags: {tagDupe.Type.Name}");
            
            ref ArchetypeData slot = ref ExistingArchetypes.GetValueRefOrAddDefault(GetHash(types, tagTypes), out bool exists);
            ArchetypeID finalID;

            if (exists)
            {
                //can't be null if entry exists
                finalID = slot!.ID;
            }
            else
            {
                int nextIDInt = ++NextArchetypeID;
                if (nextIDInt == ushort.MaxValue)
                    throw new InvalidOperationException($"Exceeded maximum unique archetype count of 65535");
                finalID = new ArchetypeID((ushort)nextIDInt);

                var arr = typesArray ?? MemoryHelpers.ReadOnlySpanToImmutableArray(types);
                var tagArr = tagTypesArray ?? MemoryHelpers.ReadOnlySpanToImmutableArray(tagTypes);

                slot = new ArchetypeData(finalID, arr, tagArr);
                ArchetypeTable.Push(slot);
                ModifyComponentLocationTable(arr, tagArr, finalID.RawIndex);
            }
            return finalID;
        }
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
            //add 1 so zero is null always
            componentTable[archetypeTypes[i].RawIndex] = (byte)(i + 1);
        }

        for (int i = 0; i < archetypeTags.Length; i++)
        {
            componentTable[archetypeTags[i].RawValue] |= GlobalWorldTables.HasTagMask;
        }
    }

    // need to be communative
    // work for n = 0
    private static ulong GetHash(ReadOnlySpan<ComponentID> types, ReadOnlySpan<TagID> andMoreTypes)
    {
        ulong hash1 = 0;
        ulong hash2 = 0;

        foreach (ComponentID value in types)
        {
            hash1 += Mix(value.RawIndex + 0x9E3779B97F4A7C15UL);
        }

        foreach (TagID value in andMoreTypes)
        {
            hash2 += Mix(value.RawValue + 0x9E3779B97F4A7C15UL);
        }

        // https://stackoverflow.com/a/13811549
        return hash1 ^ (hash2 + 0x9e3779b9 + (hash1 << 6) + (hash1 >> 2));
    }

    // splitmix64
    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return value;
    }
}
