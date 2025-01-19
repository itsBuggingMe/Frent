﻿namespace Frent.Core;

/// <summary>
/// A lightweight struct that represents a component type. Used for fast lookups
/// </summary>
public readonly struct ComponentID : IEquatable<ComponentID>
{
    internal ComponentID(ushort id) => ID = id;
    internal readonly ushort ID;

    /// <summary>
    /// The type of component this component ID represents
    /// </summary>
    public Type Type => Component.ComponentTable[ID].Type;

    /// <summary>
    /// Checks if this ComponentID instance represents the same ID as <paramref name="other"/>
    /// </summary>
    /// <param name="other">The ComponentID to compare against</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise</returns>
    public bool Equals(ComponentID other) => ID == other.ID;

    /// <summary>
    /// Checks if this ComponentID instance represents the same ID as <paramref name="obj"/>
    /// </summary>
    /// <param name="obj">The object to compare against</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise</returns>
    public override bool Equals(object? obj) => obj is ComponentID other && Equals(other);

    /// <summary>
    /// Gets the hash code for this ComponentID
    /// </summary>
    /// <returns>An integer hash code representing this ComponentID</returns>
    public override int GetHashCode() => ID;

    /// <summary>
    /// Checks if two ComponentID instances represent the same ID
    /// </summary>
    /// <param name="left">The first ComponentID</param>
    /// <param name="right">The second ComponentID</param>
    /// <returns><see langword="true"/> if they represent the same ID, <see langword="false"/> otherwise</returns>
    public static bool operator ==(ComponentID left, ComponentID right) => left.Equals(right);

    /// <summary>
    /// Checks if two ComponentID instances represent different IDs
    /// </summary>
    /// <param name="left">The first ComponentID</param>
    /// <param name="right">The second ComponentID</param>
    /// <returns><see langword="true"/> if they represent different IDs, <see langword="false"/> otherwise</returns>
    public static bool operator !=(ComponentID left, ComponentID right) => !left.Equals(right);
}