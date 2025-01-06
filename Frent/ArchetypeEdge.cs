using Frent.Core;
namespace Frent;
internal struct ArchetypeEdge(Archetype add, Archetype remove, Archetype addTag, Archetype removeTag)
{
    public Archetype Add = add;
    public Archetype Remove = remove;
    public Archetype AddTag = addTag;
    public Archetype RemoveTag = removeTag;
}