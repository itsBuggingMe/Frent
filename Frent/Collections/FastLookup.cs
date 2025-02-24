using Frent.Core;
using Frent.Core.Structures;
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
    private int index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArchetypeID FindAdjacentArchetypeID<T>(T id, ArchetypeID archetype, World world, ArchetypeEdgeType edgeType)
        where T : ITypeID
    {
        uint key = GetKey(id.Value, archetype);
        ArchetypeEdgeKey edgeKey;
        int index = LookupIndex(key);
        if (index != 32)
        {
            return new ArchetypeID(InlineArray8<ushort>.Get(ref _ids, index));
        }
        else if (world.ArchetypeGraphEdges.TryGetValue(edgeKey = typeof(T) == typeof(ComponentID) ? 
            ArchetypeEdgeKey.Component(new(id.Value), archetype, edgeType) : 
            ArchetypeEdgeKey.Tag(new(id.Value), archetype, edgeType), out var destination))
        {
            //warm/cool depending on number of times they add/remove
            return destination.ID;
        }
        //cold path
        Archetype dest = Archetype.GetAdjacentArchetypeCold(world, edgeKey);
        world.ArchetypeGraphEdges.Add(edgeKey, dest);
        return dest.ID;
    }

    public uint GetKey(ushort id, ArchetypeID archetypeID)
    {
        uint key = archetypeID.RawIndex | ((uint)id << 16);
        return key;
    }

    public void SetArchetype(ushort id, ArchetypeID from, Archetype to)
    {
        uint key = GetKey(id, from);

        InlineArray8<uint>.Get(ref _data, index) = key;
        InlineArray8<ushort>.Get(ref _ids, index) = to.ID.RawIndex;

        Archetypes[index] = to;

        index = (index + 1) & 7;
    }

    public int LookupIndex(uint key)
    {
#if NET7_0_OR_GREATER
        if(Vector256.IsHardwareAccelerated)
        {
            Vector256<uint> bits = Vector256.Equals(Vector256.Create(key), Vector256.LoadUnsafe(ref _data._0));
            int index = BitOperations.TrailingZeroCount(bits.ExtractMostSignificantBits());
            return index;
        }
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

        if (_data._0 == key)
            return 0;
        if (_data._1 == key)
            return 1;
        if (_data._2 == key)
            return 2;
        if (_data._3 == key)
            return 3;
        if (_data._4 == key)
            return 4;
        if (_data._5 == key)
            return 5;
        if (_data._6 == key)
            return 6;
        if (_data._7 == key)
            return 7;
        return 32;
    }
}