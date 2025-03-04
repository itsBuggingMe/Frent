namespace Frent.Core;

/// <summary>
/// Represents a specific type as a tag, and can be used for tag related queries
/// </summary>
public readonly struct TagID : ITypeID, IEquatable<TagID>
{
    internal TagID(ushort id) => RawValue = id;
    internal readonly ushort RawValue;

    /// <summary>
    /// The type that this TagID represents
    /// </summary>
    public Type Type => Tag.TagTable[RawValue];

    ushort ITypeID.Value => RawValue;

    /// <summary>
    /// Checks if this TagID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The tag to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public readonly bool Equals(TagID other) => other.RawValue == RawValue;
    /// <summary>
    /// Checks if this TagID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The tag to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public override bool Equals(object? other) => other is TagID t && RawValue == t.RawValue;
    /// <summary>
    /// Checks if two <see cref="TagID"/>s represent the same type
    /// </summary>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public static bool operator ==(TagID left, TagID right) => left.RawValue == right.RawValue;
    /// <summary>
    /// Checks if two <see cref="TagID"/>s represent a different type
    /// </summary>
    /// <returns><see langword="false"/> when they represent the same type, <see langword="true"/> otherwise</returns>
    public static bool operator !=(TagID left, TagID right) => left.RawValue != right.RawValue;
    /// <summary>
    /// Gets the hashcode of this <see cref="TagID"/>
    /// </summary>
    /// <returns>A unique code representing the <see cref="TagID"/></returns>
    public override int GetHashCode() => RawValue;
}
