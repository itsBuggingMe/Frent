using System.Collections.Immutable;
namespace Frent.Core;
internal record class ArchetypeData(ArchetypeID ID, ImmutableArray<Type> ComponentTypes, ImmutableArray<Type> TagTypes, int MaxChunkSize);