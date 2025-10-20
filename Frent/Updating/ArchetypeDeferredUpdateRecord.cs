using Frent.Core;
namespace Frent.Updating;

internal record struct ArchetypeDeferredUpdateRecord(Archetype Archetype, Archetype TemporaryBuffers, int InitalEntityCount);