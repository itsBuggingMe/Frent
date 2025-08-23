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
        .Where(m => m.Name == nameof(World.Create))
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private static readonly MethodInfo[] _add =
        typeof(Entity)
        .GetMethods()
        .Where(m => m.Name == nameof(Entity.Add))
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private static readonly MethodInfo[] _remove =
        typeof(Entity)
        .GetMethods()
        .Where(m => m.Name == nameof(Entity.Remove))
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private static readonly MethodInfo[] _tag =
        typeof(Entity)
        .GetMethods()
        .Where(m => m.Name == nameof(Entity.Tag))
        .Where(m => m.IsGenericMethod)
        .ToArray();

    private static readonly MethodInfo[] _detach =
        typeof(Entity)
        .GetMethods()
        .Where(m => m.Name == nameof(Entity.Detach))
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
    private StepRecord CreateGeneric()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);
        
        MethodInfo m = _create[componentParams.Length - 1].MakeGenericMethod(types);

        Entity entity= (Entity)m.Invoke(_worldState, componentParams)!;

        _componentValues.Add(entity, handles);
        _tagValues.Add(entity, []);

        return new StepRecord(entity, new { GenericArguments = types });
    }

    private StepRecord CreateHandles()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);

        Entity entity = _worldState.CreateFromHandles(CollectionsMarshal.AsSpan(handles));

        _componentValues.Add(entity, handles);
        _tagValues.Add(entity, []);

        return new StepRecord(entity, new { Types = types });
    }

    private StepRecord CreateObjects()
    {
        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types);

        Entity entity = _worldState.CreateFromObjects(componentParams);

        _componentValues.Add(entity, handles);
        _tagValues.Add(entity, []);

        return new StepRecord(entity, new { Types = types });
    }
    private StepRecord Delete()
    {
        if (!TryPickEntity(out var toDelete))
            return SkipRecord();

        toDelete.Delete();

        return new StepRecord(toDelete, new { Skip = "Deleted" }, () =>
        {
            _componentValues.Remove(toDelete);
            _tagValues.Remove(toDelete);
            _dead.Add(toDelete)
                .Assert(this);
        });
    }
    private StepRecord AddGeneric()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord();

        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types, c => !HasComponent(toAdd, c));

        if (componentParams.Length == 0)
            return new StepRecord(toAdd, new { Skip = "Not valid for the entity." });

        MethodInfo addComponentMethod = _add[componentParams.Length - 1].MakeGenericMethod(types);
        addComponentMethod.Invoke(toAdd, componentParams);

        return new(toAdd, new { GenericTypes = types }, () =>
        {
            _componentValues[toAdd].AddRange(handles);
        });
    }

    private StepRecord AddHandles()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord();

        PrepareComponents(out object[] componentParams, out List<ComponentHandle> handles, out Type[] types, c => !HasComponent(toAdd, c));

        toAdd.AddFromHandles(CollectionsMarshal.AsSpan(handles));

        return new(toAdd, new { Types = types }, () =>
        {
            _componentValues[toAdd].AddRange(handles);
        });
    }

    private StepRecord AddObject()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord();

        PrepareComponent(out object component, out ComponentHandle handle, out Type type);

        if(HasComponent(toAdd, handle.ComponentID))
            return new(toAdd, new { Skip = "Already Has Component" });

        toAdd.AddBoxed(component);

        return new(toAdd, new { Type = type }, () =>
        {
            _componentValues[toAdd].Add(handle);
        });
    }

    private StepRecord AddAs()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord();

        PrepareComponent(out object component, out ComponentHandle handle, out Type type);
        handle.Dispose();

        if(HasComponent(toAdd, Component<object>.ID))
            return new(toAdd, new { Skip = "Already Has Component" });

        toAdd.AddAs(typeof(object), component);
        handle = ComponentHandle.Create<object>(component);

        return new(toAdd, new { Type = type }, () =>
        {
            _componentValues[toAdd].Add(handle);
        });
    }

    private StepRecord RemoveGeneric()
    {
        if (!TryPickEntity(out var toAdd))
            return SkipRecord();

        List<ComponentHandle> comps = _componentValues[toAdd];
        if (comps.Count == 0)
            return new StepRecord(toAdd, new { Skip = "Not valid for the entity." });

        var types = comps.Select(h => h.ComponentID.Type).ToArray();
        _random.Shuffle(types);
        types = types.Take(_random.Next(1, comps.Count)).ToArray();

        MethodInfo addComponentMethod = _remove[types.Length - 1].MakeGenericMethod(types);
        addComponentMethod.Invoke(toAdd, []);

        return new(toAdd, new { GenericTypes = types }, () =>
        {
            if (_dead.Contains(toAdd))
                return;

            _componentValues[toAdd].RemoveAll(p => types.Any(t => p.Type == t));
        });
    }
    private StepRecord RemoveType()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        List<ComponentHandle> comps = _componentValues[e];
        if (comps.Count == 0)
            return new StepRecord(e, new { Skip = "No components to remove" });

        // Pick a single component type to remove
        var picked = comps[_random.Next(comps.Count)];
        var type = picked.ComponentID.Type;

        e.Remove(type);

        return new(e, new { RemovedType = type }, () =>
        {
            _componentValues[e].RemoveAll(h => h.Type == type);
        });
    }

    private StepRecord TagGeneric()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        var chosen = _tags.Shuffle(_random).Take(_random.Next(1, 8)).ToArray();

        var newTags = chosen.Where(t => !e.Tagged(t)).ToArray();
        if (newTags.Length == 0)
            return new(e, new { Skip = "Already has all chosen tags" });

        MethodInfo tagMethod = _tag[newTags.Length - 1].MakeGenericMethod(newTags.Select(t => t.Type).ToArray());
        tagMethod.Invoke(e, null);

        return new(e, new { Tags = newTags.Select(t => t.Type).ToArray() }, () => _tagValues[e].AddRange(newTags));
    }

    private StepRecord TagType()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        var tag = _tags[_random.Next(_tags.Length)];

        if (e.Tagged(tag))
            return new(e, new { Skip = "Already has tag" });

        e.Tag(tag.Type);

        return new(e, new { Tag = tag.Type }, () => _tagValues[e].Add(tag));
    }

    private StepRecord DetachGeneric()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        var types = _tags.Select(h => h.Type)
                         .Shuffle(_random)
                         .Take(_random.Next(1, 8))
                         .Where(t => e.Tagged(t))
                         .ToArray();

        if (types.Length == 0)
            return new(e, new { Skip = "None selected to remove." });

        MethodInfo detachMethod = _detach[types.Length - 1].MakeGenericMethod(types);
        detachMethod.Invoke(e, null);

        return new(e, new { Detached = types }, () => _tagValues[e].RemoveAll(p => types.Any(t => p.Type == t)));
    }

    private StepRecord DetachType()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        var tag = _tags[_random.Next(_tags.Length)];

        if (!e.Tagged(tag))
            return new(e, new { Skip = "Does not have tag" });

        e.Detach(tag.Type);

        return new(e, new { Detach = tag.Type }, () => _tagValues[e].Remove(tag));
    }

    private StepRecord Set()
    {
        if (!TryPickEntity(out var e))
            return SkipRecord();

        _random.Shuffle(_sharedIDs);

        if (!_componentValues[e].Any(e => _sharedIDs[0].ID == e.ComponentID))
            return new(e, new { Skip = "None selected to set." });

        ComponentHandle handle = _sharedIDs[0].Factory(_random);
        e.Set(handle.ComponentID, handle.RetrieveBoxed());

        return new(e, new { Set = handle.ComponentID.Type }, () =>
        {
            List<ComponentHandle> componentHandles = _componentValues[e];

            _componentValues[e] = componentHandles.Where(h => h.ComponentID != handle.ComponentID).Append(handle).ToList();
        });
    }
    private bool HasComponent(Entity entity, ComponentID componentId)
    {
        return _componentValues[entity].Any(c => c.ComponentID == componentId);
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

    private StepRecord SkipRecord()
    {
        return new StepRecord(default, new { Skip = "Nothing to act on." });
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
