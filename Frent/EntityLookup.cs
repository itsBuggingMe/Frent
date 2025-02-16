using System.Diagnostics;

namespace Frent;

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal record struct EntityLookup(EntityLocation Location, ushort Version)
{
    internal EntityLocation Location = Location;
    internal ushort Version = Version;
    private readonly string DebuggerDisplayString => $"Archetype {Location.ArchetypeID}, Component: {Location.Index}, Version: {Version}";
}