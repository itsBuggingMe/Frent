using System.Collections.Immutable;
namespace Frent.Core.Structures;
internal record struct ArchetypeData(ArchetypeID ID, ImmutableArray<ComponentID> ComponentTypes, ImmutableArray<TagID> TagTypes);