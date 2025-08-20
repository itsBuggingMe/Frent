using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frent.Fuzzing.Runner;

internal partial class WorldState
{
    private WorldState()
    {

    }

    private StepRecord CreateGeneric()
    {
        return default;
    }

    private StepRecord CreateHandles() => new(WorldActions.CreateHandles, default, "Not implemented");
    private StepRecord CreateObjects() => new(WorldActions.CreateObjects, default, "Not implemented");
    private StepRecord Delete() => new(WorldActions.Delete, default, "Not implemented");
    private StepRecord AddGeneric() => new(WorldActions.AddGeneric, default, "Not implemented");
    private StepRecord AddHandles() => new(WorldActions.AddHandles, default, "Not implemented");
    private StepRecord AddObject() => new(WorldActions.AddObject, default, "Not implemented");
    private StepRecord AddAs() => new(WorldActions.AddAs, default, "Not implemented");
    private StepRecord RemoveGeneric() => new(WorldActions.RemoveGeneric, default, "Not implemented");
    private StepRecord RemoveType() => new(WorldActions.RemoveType, default, "Not implemented");
    private StepRecord TagGeneric() => new(WorldActions.TagGeneric, default, "Not implemented");
    private StepRecord TagType() => new(WorldActions.TagType, default, "Not implemented");
    private StepRecord DetachGeneric() => new(WorldActions.DetachGeneric, default, "Not implemented");
    private StepRecord DetachType() => new(WorldActions.DetachType, default, "Not implemented");
    private StepRecord Set() => new(WorldActions.Set, default, "Not implemented");
    private StepRecord SubscribeWorldCreate() => new(WorldActions.SubscribeWorldCreate, default, "Not implemented");
    private StepRecord SubscribeWorldDelete() => new(WorldActions.SubscribeWorldDelete, default, "Not implemented");
    private StepRecord SubscribeAdd() => new(WorldActions.SubscribeAdd, default, "Not implemented");
    private StepRecord SubscribeRemoved() => new(WorldActions.SubscribeRemoved, default, "Not implemented");
    private StepRecord SubscribeAddGeneric() => new(WorldActions.SubscribeAddGeneric, default, "Not implemented");
    private StepRecord SubscribeRemovedGeneric() => new(WorldActions.SubscribeRemovedGeneric, default, "Not implemented");
    private StepRecord SubscribeWorldAdd() => new(WorldActions.SubscribeWorldAdd, default, "Not implemented");
    private StepRecord SubscribeWorldRemoved() => new(WorldActions.SubscribeWorldRemoved, default, "Not implemented");
    private StepRecord SubscribeTag() => new(WorldActions.SubscribeTag, default, "Not implemented");
    private StepRecord SubscribeDetach() => new(WorldActions.SubscribeDetach, default, "Not implemented");
    private StepRecord SubscribeWorldTag() => new(WorldActions.SubscribeWorldTag, default, "Not implemented");
    private StepRecord SubscribeWorldDetach() => new(WorldActions.SubscribeWorldDetach, default, "Not implemented");
    private StepRecord SubscribeDelete() => new(WorldActions.SubscribeDelete, default, "Not implemented");
}
