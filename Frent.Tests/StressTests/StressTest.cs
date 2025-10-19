using System.Reflection;
using System.Reflection.Metadata;
using Frent.Core;
using Frent.Systems;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.StressTests;

internal class StressTest
{
    [Test]
    public void Test([Range(0, 100)] int seed)
    {
        const int Steps = 1000;
        using WorldState state = new WorldState(seed);

        for(int i = 0; i < Steps; i++)
        {
            state.Advance();
        }
    }
}

internal class WorldState : IDisposable
{
    // Actions:
    // [x] Create
    // [x] Delete
    // [x] Add
    // [x] Remove
    // [ ] Tag
    // [ ] Detach

    private readonly List<Entity> _allDeletedEntities = [];
    private readonly Dictionary<Entity, List<ComponentHandle>> _componentValues = [];
    private readonly World _syncedWorld;
    private readonly Random _random;
    private readonly MethodInfo[] _create;
    private readonly Query _everythingQuery;
    private readonly int[] _shared = Enumerable.Range(0, 6).ToArray();
    private readonly TagID[] _tags = 
    [
        Tag<C1>.ID,
        Tag<C2>.ID,
        Tag<C3>.ID,
        Tag<S1>.ID,
        Tag<S2>.ID,
        Tag<S3>.ID,
    ];
    private readonly (ComponentID ID, Func<Random, ComponentHandle> Factory)[] _sharedIDs = [
        (Component<C1>.ID, (rng) => ComponentHandle.Create(new C1(rng))), 
        (Component<C2>.ID, (rng) => ComponentHandle.Create(new C2(rng))), 
        (Component<C3>.ID, (rng) => ComponentHandle.Create(new C3(rng))), 
        (Component<S1>.ID, (rng) => ComponentHandle.Create(new S1(rng))), 
        (Component<S2>.ID, (rng) => ComponentHandle.Create(new S2(rng))), 
        (Component<S3>.ID, (rng) => ComponentHandle.Create(new S3(rng)))];

    private readonly List<StressTestAction> _actions = [];

    private const int UnqiueComponentTypes = 6;

    public WorldState(int seed)
    {
        _syncedWorld = new();
        _everythingQuery = _syncedWorld.CreateQuery()
            .Build();

        _random = new Random(seed);
        _create = typeof(World).GetMethods().Where(m => m.Name == "Create").Where(m => m.IsGenericMethod).ToArray();
    }

    public void Advance()
    {
        switch(_random.Next(6))
        {
            case 0: CreateEntity(); break;
            case 1: DeleteEntity(); break;
            case 2: AddComponent(); break;
            case 3: RemoveComponent(); break;
            case 4: Tag(); break;
            case 5: Detach(); break;

            default: throw new NotImplementedException();
        }

        EnsureStateConsisstent();
    }

    public void DeleteEntity()
    {
        if(_componentValues.Count > 0)
        {
            (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();

            entity.Delete();
            _allDeletedEntities.Add(entity);
            foreach(var handle in handles)
                handle.Dispose();

            _componentValues.Remove(entity);

            _actions.Add(new StressTestAction(StressTestActionType.Delete, entity));
        }
    }

    public void CreateEntity()
    {
        int componentCount = _random.Next(UnqiueComponentTypes) + 1;
        object[] componentParams = new object[componentCount];
        _random.Shuffle(_sharedIDs);

        for(int i = 0; i < componentCount; i++)
        {
            using var handle = _sharedIDs[i].Factory(_random);
            componentParams[i] = handle.RetrieveBoxed();
        }
        
        var entity = (_random.Next() & 1) == 0 ? CreateGeneric(componentParams) : CreateBoxed(componentParams);

        Type[] componentTypes = componentParams.Select(p => p.GetType()).ToArray();

        _actions.Add(new StressTestAction(StressTestActionType.Create, entity, componentTypes));
    }

    public void RemoveComponent()
    {
        if (_componentValues.Count == 0)
            return;
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();
        if(handles.Count == 0)
            return;

        using var compHandleToRemove = handles[_random.Next(handles.Count)];
        entity.Remove(compHandleToRemove.ComponentID);
        handles.Remove(compHandleToRemove);

        _actions.Add(new StressTestAction(StressTestActionType.Remove, entity, compHandleToRemove.Type));
    }

    public void AddComponent()
    {
        if (_componentValues.Count == 0)
            return;
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();
        if(handles.Count == UnqiueComponentTypes)
            return;
        _random.Shuffle(_sharedIDs);
        foreach(var (id, fac) in _sharedIDs)
        {
            if(!entity.Has(id))
            {
                var handle = fac(_random);
                entity.AddAs(handle.Type, handle.RetrieveBoxed());
                handles.Add(handle);
                _actions.Add(new StressTestAction(StressTestActionType.Add, entity, handle.Type));
                break;
            }
        }
    }

    public void Tag()
    {
        if (_componentValues.Count == 0)
            return;
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();

        _random.Shuffle(_tags);

        TagID tag = _tags[0];

        if(!entity.Tagged(tag))
        {
            entity.Tag(tag);

            _actions.Add(new StressTestAction(StressTestActionType.Tag, entity, tag.Type));
        }
    }

    public void Detach()
    {
        if (_componentValues.Count == 0)
            return;
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();

        _random.Shuffle(_tags);

        TagID tag = _tags[0];

        if (entity.Tagged(tag))
        {
            entity.Detach(tag);

            _actions.Add(new StressTestAction(StressTestActionType.Detach, entity, tag.Type));
        }
    }

    #region Helpers

    private (Entity Entity, List<ComponentHandle> Handles) GetRandomExistingEntity()
    {
        var kvp = _componentValues.ElementAt(_random.Next(_componentValues.Count));
        return (kvp.Key, kvp.Value);
    }

    private Entity CreateGeneric(params object[] objects)
    {
        Type[] types = objects.Select(o => o.GetType()).ToArray();
        MethodInfo m = _create[objects.Length - 1].MakeGenericMethod(types);
        object? boxedEntity = m.Invoke(_syncedWorld, objects);
        That(boxedEntity, Is.Not.Null);
        var entity = (Entity)boxedEntity!;

        List<ComponentHandle> handles = new(objects.Length);

        foreach(var comp in objects)
            handles.Add(ComponentHandle.CreateFromBoxed(comp));
        
        _componentValues.Add(entity, handles);

        return entity;
    }

    private Entity CreateBoxed(params object[] objects)
    {
        var entity =_syncedWorld.CreateFromObjects(objects);
        List<ComponentHandle> handles = new(objects.Length);

        foreach(var comp in objects)
            handles.Add(ComponentHandle.CreateFromBoxed(comp));
        
        _componentValues.Add(entity, handles);
        return entity;
    }
    
    private void EnsureStateConsisstent()
    {
        That(_allDeletedEntities.All(e => !e.IsAlive));
        foreach((Entity entity, List<ComponentHandle> components) in _componentValues)
        {
            That(entity.IsAlive);
            That(!entity.IsNull);
            foreach(var comp in components)
            {
                var exp = comp.RetrieveBoxed();
                var res = entity.Get(comp.ComponentID);
                That(res, Is.EqualTo(exp));
                That(entity.Has(comp.ComponentID));
            }
        }

        int entityCount = _everythingQuery 
            .EntityCount();
        That(entityCount, Is.EqualTo(_componentValues.Count));
    }

    public void Dispose()
    {
        _syncedWorld.Dispose();
    }
    #endregion Helpers

    #region Components
    internal struct S1(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }
    internal struct S2(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }
    internal struct S3(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }

    internal class C1(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }
    internal class C2(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }
    internal class C3(Random Random)
    {
        public int Value = Random.Next();
        public override string ToString() => Value.ToString();
    }
    #endregion Components

    internal record struct StressTestAction(StressTestActionType Type, Entity Entity, params Type[] ComponentType);

    internal enum StressTestActionType
    {
        Create,
        Delete,
        Add,
        Remove,
        Tag,
        Detach,
    }
}