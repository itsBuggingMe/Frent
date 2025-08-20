namespace Frent.Fuzzing.Runner;
internal readonly record struct StepRecord(WorldActions Action, Entity Entity, object Meta, Action? Playback = null);