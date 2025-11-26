using System.Collections.Immutable;
namespace Frent.Core.Archetypes;

internal record struct ArchetypeData(ArchetypeID ID, ImmutableArray<ComponentID> ComponentTypes, ImmutableArray<TagID> TagTypes);