using Frent.Core;
namespace Frent;
internal struct ArchetypeEdge(Archetype add, Archetype remove)
{
    public Archetype Add = add;
    public Archetype Remove = remove;
}