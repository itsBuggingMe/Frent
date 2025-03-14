using System.Reflection;
using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.StressTests;

internal class StressTest
{
    
}

internal class WorldState
{
    private readonly List<Entity> _allDeletedEntities = [];
    private readonly Dictionary<Entity, List<ComponentHandle>> _componentValues = [];
    private readonly World _syncedWorld;
    private readonly Random _random;
    private readonly MethodInfo[] _create;
    private readonly int[] _shared = Enumerable.Range(0, 6).ToArray();
    private readonly (ComponentID ID, Func<Random, ComponentHandle> Factory)[] _sharedIDs = [
        (Component<C1>.ID, (rng) => ComponentHandle.Create(new C1(rng))), 
        (Component<C2>.ID, (rng) => ComponentHandle.Create(new C2(rng))), 
        (Component<C3>.ID, (rng) => ComponentHandle.Create(new C3(rng))), 
        (Component<S1>.ID, (rng) => ComponentHandle.Create(new S1(rng))), 
        (Component<S2>.ID, (rng) => ComponentHandle.Create(new S2(rng))), 
        (Component<S3>.ID, (rng) => ComponentHandle.Create(new S3(rng)))];

    private const int UnqiueComponentTypes = 6;

    public WorldState(int seed)
    {
        _syncedWorld = new();
        _random = new Random(seed);
        _create = typeof(World).GetMethods().Where(m => m.Name == "Create").ToArray();
    }

    public void DeleteEntity()
    {
        if(_componentValues.Count >= 0)
        {
            (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();

            entity.Delete();
            _allDeletedEntities.Add(entity);
            foreach(var handle in handles)
                handle.Dispose();
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
        
        _ = (_random.Next() & 1) == 0 ? CreateGeneric(componentParams) : CreateBoxed(componentParams);

        EnsureStateConsisstent();
    }

    public void RemoveComponent()
    {
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();
        if(handles.Count == 0)
            return;

        using var compHandleToRemove = handles[_random.Next(handles.Count)];
        entity.Remove(compHandleToRemove.ComponentID);

        EnsureStateConsisstent();
    }

    public void AddComponent()
    {
        (Entity entity, List<ComponentHandle> handles) = GetRandomExistingEntity();
        if(handles.Count == UnqiueComponentTypes)
            return;
        _random.Shuffle(_sharedIDs);
        foreach(var (id, fac) in _sharedIDs)
        {
            if(!entity.Has(id))
            {
                var handle = fac(_random);
                entity.Add(handle.Type, handle.RetrieveBoxed());
                handles.Add(handle);
                break;
            }
        }

        EnsureStateConsisstent();
    }

    #region  Helpers
    
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
            handles.Add(ComponentHandle.CreateFromBoxed(handles));
        
        _componentValues.Add(entity, handles);

        return entity;
    }

    private Entity CreateBoxed(params object[] objects)
    {
        var entity =_syncedWorld.CreateFromObjects(objects);
        List<ComponentHandle> handles = new(objects.Length);

        foreach(var comp in objects)
            handles.Add(ComponentHandle.CreateFromBoxed(handles));
        
        _componentValues.Add(entity, handles);
        return entity;
    }
    
    private void EnsureStateConsisstent()
    {
        That(!_allDeletedEntities.All(e => !e.IsAlive));
        foreach((Entity entity, List<ComponentHandle> components) in _componentValues)
        {
            That(entity.IsAlive);
            That(!entity.IsNull);
            foreach(var comp in components)
            {
                That(comp.RetrieveBoxed(), Is.EqualTo(entity.Get(comp.ComponentID)));
                That(entity.Has(comp.ComponentID));
            }
        }
    }
    #endregion Helpers

    #region Components
    internal record struct S1(Random Random)
    {
        public int Value = Random.Next();
    }
    internal record struct S2(Random Random)
    {
        public int Value = Random.Next();
    }
    internal record struct S3(Random Random)
    {
        public int Value = Random.Next();
    }

    internal record class C1(Random Random)
    {
        public int Value = Random.Next();
    }
    internal record class C2(Random Random)
    {
        public int Value = Random.Next();
    }
    internal record class C3(Random Random)
    {
        public int Value = Random.Next();
    }
    #endregion Components
}