namespace Frent.Core;

/// <summary>
/// Represents a specific type as a tag, and can be used for tag related queries
/// </summary>
public readonly struct TagID : IEquatable<TagID>
{
    internal TagID(int value) => Value = value + 1;
    internal readonly int ID => Value - 1;
    private readonly int Value;

    /// <summary>
    /// The type that this TagID represents
    /// </summary>
    public Type Type => Component.ComponentTable[ID].Type;
    /// <summary>
    /// Checks if this TagID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The tag to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public readonly bool Equals(TagID other) => other.ID == ID;
    /// <summary>
    /// Checks if this TagID instance represents the same type as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The tag to compare against</param>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public override bool Equals(object? other) => other is TagID t && ID == t.ID;
    /// <summary>
    /// Checks if two <see cref="TagID"/>s represent the same type
    /// </summary>
    /// <returns><see langword="true"/> when they represent the same type, <see langword="false"/> otherwise</returns>
    public static bool operator ==(TagID left, TagID right) => left.ID == right.ID;
    /// <summary>
    /// Checks if two <see cref="TagID"/>s represent a different type
    /// </summary>
    /// <returns><see langword="false"/> when they represent the same type, <see langword="true"/> otherwise</returns>
    public static bool operator !=(TagID left, TagID right) => left.ID != right.ID;
    /// <summary>
    /// Gets the hashcode of this <see cref="TagID"/>
    /// </summary>
    /// <returns>A unique code representing the <see cref="TagID"/></returns>
    public override int GetHashCode() => ID;
}
