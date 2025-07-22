using Frent.Collections;
using Frent.Core.Structures;

namespace Frent.Core;

/// <summary>
/// Holds the static <see cref="TagID"/> instance for the type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type of tag this class has info about</typeparam>
public class Tag<T>
{
    /// <summary>
    /// The static tag ID instance
    /// </summary>
    public static readonly TagID ID = Tag.GetTagID(typeof(T));
}

//this entirely piggybacks on top of component
/// <summary>
/// Manages tag types.
/// </summary>
public class Tag
{
    private static readonly Dictionary<Type, TagID> ExistingTagIDs = [];
    internal static FastStack<Type> TagTable = FastStack<Type>.Create(4);

    private static int _nextTagID = -1;

    //initalize default(TagID) to point to disable
    static Tag() => GetTagID(typeof(Disable));

    /// <summary>
    /// Gets the <see cref="TagID"/> for the given type."/>
    /// </summary>
    /// <param name="type">The type to get a <see cref="TagID"/> for.</param>
    /// <returns>The tag ID.</returns>
    public static TagID GetTagID(Type type)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            if (ExistingTagIDs.TryGetValue(type, out TagID tagID))
            {
                return tagID;
            }

            int id = Interlocked.Increment(ref _nextTagID);

            if (id == ushort.MaxValue)
                throw new Exception("Exceeded max tag count of 65535");

            TagID newID = new TagID((ushort)id);
            ExistingTagIDs[type] = newID;
            TagTable.Push(type);

            GlobalWorldTables.GrowComponentTagTableIfNeeded(newID.RawValue);

            return newID;
        }
    }
}