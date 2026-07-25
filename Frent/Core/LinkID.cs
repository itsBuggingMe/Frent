namespace Frent.Core;

// Basically copied from TagID.cs

/// <summary>
/// Represents a specific type as a link, and can be used for link related queries
/// </summary>
public readonly struct LinkID : ITypeID, IEquatable<LinkID>
{
    internal LinkID(ushort id, bool isSingleIncoming, bool isSingleOutgoing)
    {
        RawValue = id;
        IsSingleIncoming = isSingleIncoming;
        IsSingleOutgoing = isSingleOutgoing;
    }
    internal readonly ushort RawValue;
    internal readonly bool IsSingleIncoming;
    internal readonly bool IsSingleOutgoing;

    /// <summary>
    /// The type that this LinkID represents
    /// </summary>
    public Type Type => Link.LinkTable[RawValue];

    ushort ITypeID.Value => RawValue;

    /// <summary>
    /// Checks if this LinkID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The link to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public readonly bool Equals(LinkID other) => other.RawValue == RawValue;
    /// <summary>
    /// Checks if this LinkID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The link to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public override bool Equals(object? other) => other is LinkID t && RawValue == t.RawValue;
    /// <summary>
    /// Checks if two <see cref="LinkID"/>s represent the same type
    /// </summary>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public static bool operator ==(LinkID left, LinkID right) => left.RawValue == right.RawValue;
    /// <summary>
    /// Checks if two <see cref="LinkID"/>s represent a different type
    /// </summary>
    /// <returns><see langword="false"/> when they represent the same type, <see langword="true"/> otherwise</returns>
    public static bool operator !=(LinkID left, LinkID right) => left.RawValue != right.RawValue;
    /// <summary>
    /// Gets the hashcode of this <see cref="LinkID"/>
    /// </summary>
    /// <returns>A unique code representing the <see cref="LinkID"/></returns>
    public override int GetHashCode() => RawValue;
}
