using Frent.Marshalling;

namespace Frent.Fuzzing.Runner;

internal struct StepRecord(Entity entity, object meta, Action? playback = null)
{
    public WorldActions Action;
    public Entity Entity = entity;
    public object Meta = meta;
    public Action? Playback = playback;
    public int Step;

    public override string ToString() => $"Action: {Action}, Entity {EntityMarshal.EntityID(Entity)}, Meta: {Meta}, Step: {Step}";
}