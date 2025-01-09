using Frent.Collections;
using Frent.Updating;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Frent.Core;

partial class Archetype
{
    internal static FastStack<ArchetypeData> ArchetypeTable = FastStack<ArchetypeData>.Create(16);
    internal static int NextArchetypeID = -1;

    private static readonly Dictionary<long, ArchetypeData> ExistingArchetypes = [];

    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<Type> types, ReadOnlySpan<Type> tagTypes, World world, ImmutableArray<Type>? typeArray = null, ImmutableArray<Type>? tagTypesArray = null)
    {
        ArchetypeID id = GetArchetypeID(types, tagTypes, typeArray, tagTypesArray);
        ref Archetype archetype = ref world.GetArchetype((uint)id.ID);
        if (archetype is not null)
            return archetype;

        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = Component.GetComponentRunnerFromType(types[i]);
        archetype = new Archetype(world, componentRunners, ArchetypeTable[id.ID]);
        world.ArchetypeAdded(archetype);
        return archetype;
    }

    //initalize default(ArchetypeID) to point to empty archetype
    static Archetype() => GetArchetypeID([], []);

    internal static ArchetypeID GetArchetypeID(ReadOnlySpan<Type> types, ReadOnlySpan<Type> tagTypes, ImmutableArray<Type>? typesArray = null, ImmutableArray<Type>? tagTypesArray = null)
    {
        ref ArchetypeData? slot = ref CollectionsMarshal.GetValueRefOrAddDefault(ExistingArchetypes, GetHash(types, tagTypes), out bool exists);
        ArchetypeID finalID;

        if (exists)
        {
            //can't be null if entry exists
            finalID = slot!.ID;
        }
        else
        {
            finalID = new ArchetypeID(Interlocked.Increment(ref NextArchetypeID));

            var arr = typesArray ?? ReadOnlySpanToImmutableArray(types);
            var tagArr = tagTypesArray ?? ReadOnlySpanToImmutableArray(tagTypes);

            slot = CreateArchetypeData(finalID, arr, tagArr);
            ArchetypeTable.Push(slot);
            ModifyComponentLocationTable(arr, tagArr, finalID.ID);
        }

        return finalID;
    }

    public static ArchetypeData CreateArchetypeData(ArchetypeID id, ImmutableArray<Type> componentTypes, ImmutableArray<Type> tagTypes)
    {
        return new ArchetypeData(id, componentTypes, tagTypes, MemoryHelpers.MaxArchetypeChunkSize);
    }

    private static void ModifyComponentLocationTable(ImmutableArray<Type> archetypeTypes, ImmutableArray<Type> archetypeTags, int id)
    {
        if (GlobalWorldTables.ComponentTagLocationTable.Length == id)
        {
            Array.Resize(ref GlobalWorldTables.ComponentTagLocationTable, Math.Max(id << 1, 1));
        }

        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            _ = Component.GetComponentID(archetypeTypes[i]);
        }

        ref var componentTable = ref GlobalWorldTables.ComponentTagLocationTable[id];
        componentTable = new byte[GlobalWorldTables.ComponentTagTableBufferSize];
        componentTable.AsSpan().Fill(Tag.DefaultNoTag);
        //TODO: tags
        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            componentTable[Component.GetComponentID(archetypeTypes[i]).ID] = (byte)i;
        }

        for(int i = 0; i < archetypeTags.Length; i++)
        {
            componentTable[Tag.GetTagID(archetypeTags[i]).ID] |= Tag.HasTagMask;
        }
    }

    private static long GetHash(ReadOnlySpan<Type> types, ReadOnlySpan<Type> andMoreTypes)
    {
        HashCode h1 = new();

        int i;
        for (i = 0; i < types.Length >> 1; i++)
        {
            h1.Add(types[i]);
        }


        int tagHash = 0;
        foreach (var item in andMoreTypes)
        {
            //we do this so its communative
            tagHash += item.GetHashCode();
        }

        h1.Add(tagHash);

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
        for (int i = 0; i < types.Length; i++)
            builder.Add(types[i]);
        return builder.MoveToImmutable();
    }
}
