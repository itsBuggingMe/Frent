using Frent.Updating;
using System.Diagnostics;

namespace Frent.Core;

/// <summary>
/// A lightweight struct that represents a component type. Used for fast lookups.
/// </summary>
[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
public readonly struct ComponentID : ITypeID, IEquatable<ComponentID>
{
    internal ComponentID(ushort id, ushort sparseIndex)
    {
        RawIndex = id;
        SparseIndex = sparseIndex;
    }
    internal readonly ushort RawIndex;
    internal readonly ushort SparseIndex;
    internal readonly bool IsSparseComponent => SparseIndex != 0;

    /// <summary>
    /// The type of component this <see cref="ComponentID"/> represents.
    /// </summary>
    public Type Type => Component.ComponentTable[RawIndex].Type;

    ushort ITypeID.Value => RawIndex;
    internal readonly UpdateMethodData[] Methods => Component.ComponentTable[RawIndex].UpdateMethods;
    internal readonly int SparseIndex => Component.ComponentTable[RawIndex].SparseComponentIndex;
    internal readonly bool IsSparseComponent => SparseIndex == 0;

    /// <summary>
    /// Checks if this <see cref="ComponentID"/> instance represents the same ID as <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="ComponentID"/> to compare against.</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise.</returns>
    public bool Equals(ComponentID other) => RawIndex == other.RawIndex;

    /// <summary>
    /// Checks if this <see cref="ComponentID"/> instance represents the same ID as <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise.</returns>
    public override bool Equals(object? obj) => obj is ComponentID other && Equals(other);

    /// <summary>
    /// Gets the hash code for this <see cref="ComponentID"/>.
    /// </summary>
    /// <returns>An integer hash code representing this <see cref="ComponentID"/>.</returns>
    public override int GetHashCode() => RawIndex;

    /// <summary>
    /// Checks if two <see cref="ComponentID"/> instances represent the same ID.
    /// </summary>
    /// <param name="left">The first <see cref="ComponentID"/>.</param>
    /// <param name="right">The second <see cref="ComponentID"/>.</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise.</returns>
    public static bool operator ==(ComponentID left, ComponentID right) => left.Equals(right);

    /// <summary>
    /// Checks if two <see cref="ComponentID"/> instances represent different IDs.
    /// </summary>
    /// <param name="left">The first <see cref="ComponentID"/>.</param>
    /// <param name="right">The second <see cref="ComponentID"/>.</param>
    /// <returns><see langword="true"/> if they represent different IDs, <see langword="false"/> otherwise.</returns>
    public static bool operator !=(ComponentID left, ComponentID right) => !left.Equals(right);

    internal string DebuggerDisplayString => $"Types: {Type} ID: {RawIndex}";
}