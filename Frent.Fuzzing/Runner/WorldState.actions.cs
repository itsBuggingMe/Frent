using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using Frent.Core;

namespace Frent.Fuzzing.Runner;

internal partial class WorldState
{
    private static readonly MethodInfo[] _create = 
        typeof(World)
        .GetMethods()
        .Where(m => m.Name == "Create")
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private static readonly MethodInfo[] _add =
        typeof(Entity)
        .GetMethods()
        .Where(m => m.Name == "Add")
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private readonly int[] _shared = Enumerable.Range(0, 6).ToArray();
    private readonly TagID[] _tags =
    [
        Tag<C1>.ID,
        Tag<C2>.ID,
        Tag<C3>.ID,
        Tag<C4>.ID,
        Tag<S1>.ID,
        Tag<S2>.ID,
        Tag<S4>.ID,
    ];
    private readonly (ComponentID ID, Func<Random, ComponentHandle> Factory)[] _sharedIDs = [
        (Component<C1>.ID, (rng) => ComponentHandle.Create(new C1(rng.Next()))),
        (Component<C2>.ID, (rng) => ComponentHandle.Create(new C2(rng.Next()))),
        (Component<C3>.ID, (rng) => ComponentHandle.Create(new C3(rng.Next()))),
        (Component<C4>.ID, (rng) => ComponentHandle.Create(new C4(rng.Next()))),
        (Component<S1>.ID, (rng) => ComponentHandle.Create(new S1(rng.Next()))),
        (Component<S2>.ID, (rng) => ComponentHandle.Create(new S2(rng.Next()))),
        (Component<S3>.ID, (rng) => ComponentHandle.Create(new S3(rng.Next()))),
        (Component<S4>.ID, (rng) => ComponentHandle.Create(new S4(rng.Next()))),
        ];

    private WorldState()
    {

    }

    private StepRecord CreateGeneric()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);
        
        MethodInfo m = _create[componentParams.Length - 1].MakeGenericMethod(types);

        Entity entity= (Entity)m.Invoke(_worldState, componentParams)!;

        return new StepRecord(WorldActions.CreateGeneric, entity, new { GenericArguments = types }, () => _componentValues.Add(entity, handles));
    }

    private StepRecord CreateHandles()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);

        Entity entity = _worldState.CreateFromHandles(CollectionsMarshal.AsSpan(handles));

        return new StepRecord(WorldActions.CreateHandles, entity, new { Types = types }, () => _componentValues.Add(entity, handles));
    }

    private StepRecord CreateObjects()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);

        Entity entity = _worldState.CreateFromObjects(componentParams);

        return new StepRecord(WorldActions.CreateObjects, entity, new { Types = types }, () => _componentValues.Add(entity, handles));
    }
    private StepRecord Delete()
    {
        if (!TryPickEntity(out var toDelete))
            return SkipRecord(WorldActions.Delete);

        toDelete.Delete();

        return new StepRecord(WorldActions.Delete, toDelete, new { Skip = "Deleted" }, () =>
        {
            _componentValues.Remove(toDelete);
            _dead.Add(toDelete)
                .Assert(this);
        });
    }
    private StepRecord AddGeneric()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord(WorldActions.AddGeneric);

        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types, c => !_componentValues[toAdd].Any(h => h.ComponentID == c));

        MethodInfo addComponentMethod = _add[componentParams.Length - 1].MakeGenericMethod(types);
        addComponentMethod.Invoke(toAdd, componentParams);

        return new(WorldActions.AddGeneric, toAdd, new { GenericTypes = types }, () =>
        {
            if (_dead.Contains(toAdd))
                return;

            _componentValues[toAdd].AddRange(handles);
        });
    }

    private StepRecord AddHandles()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord(WorldActions.AddHandles);

        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types, c => !_componentValues[toAdd].Any(h => h.ComponentID == c));

        toAdd.AddFromHandles(CollectionsMarshal.AsSpan(handles));

        return new(WorldActions.AddHandles, toAdd, new { Types = types }, () =>
        {
            if (_dead.Contains(toAdd))
                return;

            _componentValues[toAdd].AddRange(handles);
        });
    }

    private StepRecord AddObject()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord(WorldActions.AddObject);

        PrepareComponent(out object component, out ComponentHandle handle, out Type type);

        if(!_componentValues[toAdd].Any(h => h.ComponentID == handle.ComponentID))
            return new(WorldActions.AddHandles, toAdd, new { Skip = "Already Has Component" });

        toAdd.AddBoxed(component);

        return new(WorldActions.AddHandles, toAdd, new { Type = type }, () =>
        {
            if (_dead.Contains(toAdd))
                return;

            _componentValues[toAdd].Add(handle);
        });
    }

    private StepRecord AddAs()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord(WorldActions.AddObject);

        PrepareComponent(out object component, out ComponentHandle handle, out Type type);
        handle.Dispose();

        toAdd.AddAs(typeof(object), component);
        handle = ComponentHandle.Create<object>(component);

        return new(WorldActions.AddHandles, toAdd, new { Type = type }, () =>
        {
            if (_dead.Contains(toAdd))
                return;

            _componentValues[toAdd].Add(handle);
        });
    }

    private StepRecord RemoveGeneric()
    {
        return new(WorldActions.RemoveGeneric, default, "Not implemented");
    }
    private StepRecord RemoveType()
    {
        return new(WorldActions.RemoveType, default, "Not implemented");
    }
    private StepRecord TagGeneric()
    {
        return new(WorldActions.TagGeneric, default, "Not implemented");
    }
    private StepRecord TagType()
    {
        return new(WorldActions.TagType, default, "Not implemented");
    }
    private StepRecord DetachGeneric()
    {
        return new(WorldActions.DetachGeneric, default, "Not implemented");
    }
    private StepRecord DetachType()
    {
        return new(WorldActions.DetachType, default, "Not implemented");
    }
    private StepRecord Set()
    {
        return new(WorldActions.Set, default, "Not implemented");
    }
    private StepRecord SubscribeWorldCreate()
    {
        return new(WorldActions.SubscribeWorldCreate, default, "Not implemented");
    }
    private StepRecord SubscribeWorldDelete()
    {
        return new(WorldActions.SubscribeWorldDelete, default, "Not implemented");
    }
    private StepRecord SubscribeAdd()
    {
        return new(WorldActions.SubscribeAdd, default, "Not implemented");
    }
    private StepRecord SubscribeRemoved()
    {
        return new(WorldActions.SubscribeRemoved, default, "Not implemented");
    }
    private StepRecord SubscribeAddGeneric()
    {
        return new(WorldActions.SubscribeAddGeneric, default, "Not implemented");
    }
    private StepRecord SubscribeRemovedGeneric()
    {
        return new(WorldActions.SubscribeRemovedGeneric, default, "Not implemented");
    }
    private StepRecord SubscribeWorldAdd()
    {
        return new(WorldActions.SubscribeWorldAdd, default, "Not implemented");
    }
    private StepRecord SubscribeWorldRemoved()
    {
        return new(WorldActions.SubscribeWorldRemoved, default, "Not implemented");
    }
    private StepRecord SubscribeTag()
    {
        return new(WorldActions.SubscribeTag, default, "Not implemented");
    }
    private StepRecord SubscribeDetach()
    {
        return new(WorldActions.SubscribeDetach, default, "Not implemented");
    }
    private StepRecord SubscribeWorldTag()
    {
        return new(WorldActions.SubscribeWorldTag, default, "Not implemented");
    }
    private StepRecord SubscribeWorldDetach()
    {
        return new(WorldActions.SubscribeWorldDetach, default, "Not implemented");
    }
    private StepRecord SubscribeDelete()
    {
        return new(WorldActions.SubscribeDelete, default, "Not implemented");
    }

    private void PrepareComponents(out object[] componentParams, out List<ComponentHandle> componentHandles, out Type[] types, Func<ComponentID, bool>? selector = null)
    {
        int componentCount = _random.Next(_sharedIDs.Length) + 1;

        _random.Shuffle(_sharedIDs);

        var filtered = _sharedIDs
            .Take(componentCount)
            .Where(entry => selector == null || selector(entry.ID))
            .ToArray();

        componentParams = new object[filtered.Length];
        componentHandles = new(filtered.Length);

        int index = 0;
        foreach (var (id, fac) in filtered)
        {
            var handle = fac(_random);
            componentHandles.Add(handle);
            componentParams[index++] = handle.RetrieveBoxed();
        }

        types = componentParams.Select(o => o.GetType()).ToArray();
    }


    private void PrepareComponent(out object component, out ComponentHandle handle, out Type type)
    {
        _random.Shuffle(_sharedIDs);

        handle = _sharedIDs[0].Factory(_random);
        component = handle.RetrieveBoxed();
        type = component.GetType();
    }

    private StepRecord SkipRecord(WorldActions action)
    {
        return new StepRecord(action, default, new { Skip = "Nothing to act on." });
    }

    private bool TryPickEntity(out Entity e)
    {
        if (_componentValues.Count == 0)
        {
            e = default;
            return false;
        }

        e = Entities.Skip(_random.Next(_componentValues.Count)).First();
        return true;
    }
}
