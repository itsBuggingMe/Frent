using Frent.Core;
using Frent.Components;

namespace Frent.Marshalling;

/// <summary>
/// Provides access to global type registration information.
/// </summary>
/// <remarks>The APIs in this class are less stable, as they depend on implementation details.</remarks>
public static class TypeMarshal
{
    /// <summary>
    /// Gets the current number of registered component types.
    /// </summary>
    /// <remarks>The count includes default(ComponentID), which points to void.</remarks>
    public static int GetComponentTypeCount() => Component.ComponentTable.Count;

    /// <summary>
    /// Constructs a <see cref="ComponentID"/> from its raw type index.
    /// </summary>
    /// <param name="index">An index between zero and <see cref="GetComponentTypeCount"/> - 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not identify a registered component type.</exception>
    public static ComponentID CreateComponentID(ushort index)
    {
        if (index >= Component.ComponentTable.Count)
            FrentExceptions.Throw_ArgumentOutOfRangeException(nameof(index));
        return new ComponentID(index);
    }

    /// <summary>
    /// Gets the current number of registered tag types.
    /// </summary>
    /// <remarks>The count includes built-in tag types.</remarks>
    public static int GetTagTypeCount() => Tag.TagTable.Count;

    /// <summary>
    /// Constructs a <see cref="TagID"/> from its raw registered-type index.
    /// </summary>
    /// <param name="index">An index between zero and <see cref="GetTagTypeCount"/> minus one.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not identify a registered tag type.</exception>
    public static TagID CreateTagID(ushort index)
    {
        if (index >= Tag.TagTable.Count)
            FrentExceptions.Throw_ArgumentOutOfRangeException(nameof(index));
        return new TagID(index);
    }

    /// <summary>
    /// Gets the current number of registered link types.
    /// </summary>
    /// <remarks>The count includes the reserved default link type.</remarks>
    public static int GetLinkTypeCount() => Link.LinkTable.Count;

    /// <summary>
    /// Constructs a <see cref="LinkID"/> from its raw registered-type index.
    /// </summary>
    /// <param name="index">An index between zero and <see cref="GetLinkTypeCount"/> minus one.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not identify a registered link type.</exception>
    public static LinkID CreateLinkID(ushort index)
    {
        if (index >= Link.LinkTable.Count)
            FrentExceptions.Throw_ArgumentOutOfRangeException(nameof(index));
        return Link.GetLinkID(Link.LinkTable[index]);
    }
}
