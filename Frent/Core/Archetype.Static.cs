﻿using Frent.Collections;
using Frent.Updating;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Variadic.Generator;

namespace Frent.Core;

[Variadic("Archetype<T>", "Archetype<|T$, |>")]
[Variadic("typeof(T)", "|typeof(T$), |")]
[Variadic("Component<T>.ID", "|Component<T$>.ID, |")]
[Variadic("[Component<T>.CreateInstance()]", "[|Component<T$>.CreateInstance(), |]")]
internal static class Archetype<T>
{
    public static readonly ImmutableArray<Type> ArchetypeTypes = new Type[] { typeof(T) }.ToImmutableArray();
    public static readonly ImmutableArray<ComponentID> ArchetypeComponentIDs = new ComponentID[] { Component<T>.ID }.ToImmutableArray();

    //ArchetypeTypes init first, then ID
    public static readonly ArchetypeID ID = Archetype.GetArchetypeID(ArchetypeTypes.AsSpan(), [], ArchetypeTypes);
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
    internal static Archetype CreateOrGetExistingArchetype(ReadOnlySpan<Type> types, ReadOnlySpan<Type> tagTypes, World world, ImmutableArray<Type>? typeArray = null, ImmutableArray<Type>? tagTypesArray = null)
    {
        ArchetypeID id = GetArchetypeID(types, tagTypes, typeArray, tagTypesArray);
        ref Archetype archetype = ref world.WorldArchetypeTable[id.ID];
        if (archetype is not null)
            return archetype;

        IComponentRunner[] componentRunners = new IComponentRunner[types.Length];
        for (int i = 0; i < types.Length; i++)
            componentRunners[i] = Component.GetComponentRunnerFromType(types[i]);

        archetype = new Archetype(id, componentRunners);
        world.ArchetypeAdded(archetype.ID);

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

    public static ArchetypeData CreateArchetypeData(ArchetypeID id, ImmutableArray<Type> componentTypes, ImmutableArray<Type> tagTypes)
    {
        return new ArchetypeData(id, componentTypes, tagTypes);
    }

    private static void ModifyComponentLocationTable(ImmutableArray<Type> archetypeTypes, ImmutableArray<Type> archetypeTags, int id)
    {
        if (GlobalWorldTables.ComponentTagLocationTable.Length == id)
        {
            int size = Math.Max(id << 1, 1);
            Array.Resize(ref GlobalWorldTables.ComponentTagLocationTable, size);
            foreach(var world in GlobalWorldTables.Worlds.AsSpan())
            {
                if(world is World w)
                {   
                    w.UpdateArchetypeTable(size);
                }
            }
        }

        for (int i = 0; i < archetypeTypes.Length; i++)
        {
            _ = Component.GetComponentID(archetypeTypes[i]);
        }

        ref var componentTable = ref GlobalWorldTables.ComponentTagLocationTable[id];
        componentTable = new byte[GlobalWorldTables.ComponentTagTableBufferSize];
        componentTable.AsSpan().Fill(Tag.DefaultNoTag);

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
}
