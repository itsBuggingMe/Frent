using Frent.Updating;

namespace Frent.Core;

//38 bytes total - 16 header + mt, 8 comps, 8 entities, 6 ids and tracking
partial class Archetype(ArchetypeID archetypeID, IComponentRunner[] components)
{
    //8
    internal IComponentRunner[] Components = components;
    //8
    //we include version
    //this is so we dont need to lookup
    //the world table every time
    private EntityIDOnly[] _entities = new EntityIDOnly[1];
    //2
    private ArchetypeID _archetypeID = archetypeID;
    //4
    /// <summary>
    /// The next component index
    /// </summary>
    private int _componentIndex = 0;
}