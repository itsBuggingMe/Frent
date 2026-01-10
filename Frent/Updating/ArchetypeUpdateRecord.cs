using Frent.Core.Archetypes;

namespace Frent.Updating;

internal record struct ArchetypeUpdateRecord(Archetype Archetype, int Start, int Length);