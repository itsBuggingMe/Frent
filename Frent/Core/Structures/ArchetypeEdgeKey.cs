using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core.Structures;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ArchetypeEdgeKey : IEquatable<ArchetypeEdgeKey>
{
    //could be tag type or component type
    internal ComponentID ComponentID;
    internal TagID TagID;
    internal ArchetypeID ArchetypeFrom;
    internal ArchetypeEdgeType EdgeType;

    public static ArchetypeEdgeKey Component(ComponentID componentID, ArchetypeID from, ArchetypeEdgeType archetypeEdgeType) => new()
    {
        ComponentID = componentID,
        ArchetypeFrom = from,
        EdgeType = archetypeEdgeType,
    };

    public static ArchetypeEdgeKey Tag(TagID tagID, ArchetypeID from, ArchetypeEdgeType archetypeEdgeType) => new()
    {
        TagID = tagID,
        ArchetypeFrom = from,
        EdgeType = archetypeEdgeType,
    };

#if NET8_0_OR_GREATER
    internal long Packed => Unsafe.BitCast<ArchetypeEdgeKey, long>(this);
#else
    internal long Packed => Unsafe.As<ArchetypeEdgeKey, long>(ref this);
#endif
    public bool Equals(ArchetypeEdgeKey other) => other.Packed == Packed;
    public override bool Equals(object? obj) => obj is ArchetypeEdgeKey other && Equals(other);
    public override int GetHashCode() => Packed.GetHashCode();
}

internal enum ArchetypeEdgeType : ushort
{
    AddComponent = 49157,
    RemoveComponent = 24593,
    AddTag = 12289,
    RemoveTag = 6151,
}