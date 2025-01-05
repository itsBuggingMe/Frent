using System.Collections.Immutable;

namespace Frent.Core;
internal record class ArchetypeData(EntityType ID, ImmutableArray<Type> ComponentTypes, int MaxChunkSize);