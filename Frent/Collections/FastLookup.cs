using Frent.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Frent.Collections;

internal struct FastLookup()
{
    private InlineArray8<uint> _data;
    private InlineArray8<ushort> _ids;
    internal Archetype[] Archetypes = new Archetype[8];
    internal Dictionary<uint, Archetype> FallbackLookup = [];
    private int index;

    public Archetype FindAdjacentArchetype(ComponentID component, Archetype archetype, Archetype.ArchetypeStructualAction type, World world)
    {
        uint key = GetKey(component.RawIndex, archetype.ID);
        int index = LookupIndex(key);
        if (index != 32)
        {
            return Archetypes.UnsafeArrayIndex(index);
        }
        else if (FallbackLookup.TryGetValue(key, out var destination))
        {
            return destination;
        }
        return GetArchetypeSlow(world, type, archetype.ID, component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArchetypeID FindAdjacentArchetypeID(ComponentID component, ArchetypeID archetype, Archetype.ArchetypeStructualAction type, World world)
    {
        uint key = GetKey(component.RawIndex, archetype);
        int index = LookupIndex(key);
        if (index != 32)
        {
            return new ArchetypeID(InlineArray8<ushort>.Get(ref _ids, index));
        }
        else if (FallbackLookup.TryGetValue(key, out var destination))
        {
            //warm/cool depending on number of times they add/remove
            return destination.ID;
        }
        //cold path
        return GetArchetypeSlow(world, type, archetype, component).ID;
    }

    private Archetype GetArchetypeSlow(World world, Archetype.ArchetypeStructualAction transform, ArchetypeID from, ComponentID componentID)
    {
        var result = from.Archetype(world).FindArchetypeAdjacent(world, componentID, transform);
        SetArchetype(componentID.RawIndex, from, result);
        return result;
    }

    public uint GetKey(ushort id, ArchetypeID archetypeID)
    {
        uint key = archetypeID.RawIndex | ((uint)id << 16);
        return key;
    }

    public Archetype? TryGetValue(ushort id, ArchetypeID archetypeID)
    {
        uint key = archetypeID.RawIndex | ((uint)id << 16);
        int index = LookupIndex(key);
        if(index != 32)
        {
            return Archetypes.UnsafeArrayIndex(index);
        }

        if(FallbackLookup.TryGetValue(key, out Archetype? value))
        {
            return value;
        }

        return null;
    }

    public void SetArchetype(ushort id, ArchetypeID from, Archetype to)
    {
        uint key = GetKey(id, from);
        
        FallbackLookup[key] = to;

        InlineArray8<uint>.Get(ref _data, index) = key;
        InlineArray8<ushort>.Get(ref _ids, index) = to.ID.RawIndex;
        
        Archetypes[index] = to;

        index = (index + 1) & 7;
    }

    public int LookupIndex(uint key)
    {
#if NET7_0_OR_GREATER
        Vector256<uint> bits = Vector256.Equals(Vector256.Create(key), Vector256.LoadUnsafe(ref _data._0));
        int index = BitOperations.TrailingZeroCount(bits.ExtractMostSignificantBits());
        return index;
        //else if (Vector128.IsHardwareAccelerated)
        //{
        //    Vector128<uint> lower = Vector128.Equals(Vector128.Create(key), Vector128.LoadUnsafe(ref l0));
        //    Vector128<uint> upper = Vector128.Equals(Vector128.Create(key), Vector128.LoadUnsafe(ref l4));
        //
        //    uint lowerMask = lower.ExtractMostSignificantBits();
        //    uint upperMask = upper.ExtractMostSignificantBits() << 4;
        //
        //    int index = BitOperations.TrailingZeroCount(lowerMask | upperMask);
        //    return index;
        //}
#endif
        int bclIndex = MemoryMarshal.CreateSpan(ref _data._0, 8).IndexOf(key);
        return bclIndex == -1 ? 32 : bclIndex;
    }
}