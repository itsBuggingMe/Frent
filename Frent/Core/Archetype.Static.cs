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

    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<Type> types, World world, ImmutableArray<Type>? typeArray = null)
    {
        EntityType id = GetArchetypeID(types, typeArray);
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

    internal static EntityType GetArchetypeID(ReadOnlySpan<Type> types, ImmutableArray<Type>? typesArray = null)
    {
        ref ArchetypeData? slot = ref CollectionsMarshal.GetValueRefOrAddDefault(ExistingArchetypes, GetHash(types), out bool exists);
        EntityType finalID;

        if (exists)
        {
            //can't be null if entry exists
            finalID = slot!.ID;
        }
        else
        {
            finalID = new EntityType(Interlocked.Increment(ref NextArchetypeID));

            var arr = typesArray ?? ReadOnlySpanToImmutableArray(types);
            slot = CreateArchetypeData(finalID, typesArray ?? ReadOnlySpanToImmutableArray(types));
            ArchetypeTable.Push(slot);
            ModifyComponentLocationTable(arr, finalID.ID);
        }

        return finalID;
    }

    public static ArchetypeData CreateArchetypeData(EntityType id, ImmutableArray<Type> componentTypes)
    {
        //8 bytes for entity struct
        int entitySize = 8;
        foreach (var value in componentTypes)
        {
            entitySize += Component.ComponentSizes[value];
        }

        return new ArchetypeData(id, componentTypes, (int)PreformanceHelpers.RoundDownToPowerOfTwo((uint)(PreformanceHelpers.MaxArchetypeChunkSize / entitySize)));
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
            componentTable[Component.GetComponentID(archetypeTypes[i]).ID] = (byte)i;
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
        for (int i = 0; i < types.Length; i++)
            builder.Add(types[i]);
        return builder.MoveToImmutable();
    }
}
