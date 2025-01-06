global using ArchetypeID = Frent.Core.EntityType;
using System.Collections.Immutable;

namespace Frent.Core;

//This isn't named ArchetypeID because archetypes are an implementation detail
/// <summary>
/// Represents an entity's type, or set of component and tag types that make it up
/// </summary>>
public struct EntityType
{
    internal EntityType(int id) => ID = id;

    /// <summary>
    /// The component types
    /// </summary>
    public ImmutableArray<Type> Types => Archetype.ArchetypeTable[ID].ComponentTypes;
    /// <summary>
    /// The tag types
    /// </summary>
    public ImmutableArray<Type> Tags => Archetype.ArchetypeTable[ID].TagTypes;

    internal int ID;
}
