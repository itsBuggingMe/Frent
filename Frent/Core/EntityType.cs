namespace Frent.Core;

//This isn't named ArchetypeID because archetypes are an implementation detail
public struct EntityType(int id)
{
    internal int ID = id;
}
