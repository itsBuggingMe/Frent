using Frent.Core;

namespace Frent;
internal struct ArchetypeEdge
{
    public ArchetypeEdge(Archetype add, Archetype remove)
    {
        Add = add;
        Remove = remove;
    }

    public Archetype Add;
    public Archetype Remove;
}