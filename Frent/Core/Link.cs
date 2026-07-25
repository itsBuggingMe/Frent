using Frent.Collections;

namespace Frent.Core;

// Basically a copy of Tag.cs
// abstract into shared impl?

/// <summary>
/// Holds the static <see cref="LinkID"/> instance for the type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type of link this class has info about</typeparam>
public static class Link<T>
{
    /// <summary>
    /// The static link ID instance
    /// </summary>
    public static readonly LinkID ID = Link.GetLinkID(typeof(T));
}

/// <summary>
/// Manages link types.
/// </summary>
public static class Link
{
    private static readonly Dictionary<Type, LinkID> ExistingLinkIDs = [];
    private static readonly Dictionary<string, LinkID> ExistingLinkIDsByName = [];

    internal static LinkID? GetLinkType(string linkType)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            return ExistingLinkIDsByName.TryGetValue(linkType, out var value) ? value : null;
        }
    }

    internal static FastStack<Type> LinkTable = FastStack<Type>.Create(4);

    internal static int LinkTableBufferSize = 4;

    private static int _nextLinkID = -1;

    //initalize default(LinkID) to point to void
    static Link() => GetLinkID(typeof(void));

    /// <summary>
    /// Gets the <see cref="LinkID"/> for the given type.
    /// </summary>
    /// <param name="type">The type to get a <see cref="LinkID"/> for.</param>
    /// <returns>The link ID.</returns>
    public static LinkID GetLinkID(Type type)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            if (ExistingLinkIDs.TryGetValue(type, out LinkID linkID))
            {
                return linkID;
            }

            int id = Interlocked.Increment(ref _nextLinkID);

            if (id == ushort.MaxValue)
                throw new Exception("Exceeded max link count of 65535");

            LinkID newID = new LinkID((ushort)id);
            ExistingLinkIDs[type] = newID;
            ExistingLinkIDsByName[type.ToString()] = newID;
            LinkTable.Push(type);

            if (newID.RawValue >= LinkTableBufferSize)
            {
                LinkTableBufferSize = Math.Max(LinkTableBufferSize << 1, 1);
                foreach (var world in GlobalWorldTables.Worlds.AsSpan())
                    world?.GrowLinkTable(LinkTableBufferSize);
            }

            return newID;
        }
    }

    /// <summary>
    /// Register a link type and its associated metadata.
    /// </summary>
    /// <param name="type">The type of link to register.</param>
    public static void RegisterLink(Type type) => _ = GetLinkID(type);
}
