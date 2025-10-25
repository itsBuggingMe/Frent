using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseComponentQueryTests
{
    #region Test Components
    
    internal struct SparseQueryComponent : ISparseComponent
    {
        public int Value;
        
        public SparseQueryComponent(int value)
        {
            Value = value;
        }
    }
    
    internal struct SparseQueryComponent2 : ISparseComponent
    {
        public string Name;
        
        public SparseQueryComponent2(string name)
        {
            Name = name;
        }
    }
    
    internal struct RegularQueryComponent
    {
        public float Data;
        
        public RegularQueryComponent(float data)
        {
            Data = data;
        }
    }
    
    internal struct SparseUpdateQueryComponent : ISparseComponent, IUpdate
    {
        public int UpdateCount;
        
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    #endregion
    
    #region Query with Sparse Components Tests
    
    [Test]
    public void Query_WithSparseComponent_FindsEntities()
    {
        using World world = new();
        var entity1 = world.Create<SparseQueryComponent>(new SparseQueryComponent(10));
        var entity2 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        var entity3 = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }

        CollectionAssert.Contains(foundEntities, entity1);
        CollectionAssert.Contains(foundEntities, entity2);
        That(foundEntities, Does.Not.Contain(entity3));
        That(foundEntities.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void Query_WithMultipleSparseComponents_FindsMatchingEntities()
    {
        using World world = new();
        var entity1 = world.Create<SparseQueryComponent, SparseQueryComponent2>(
            new SparseQueryComponent(10), new SparseQueryComponent2("test1"));
        var entity2 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        var entity3 = world.Create<SparseQueryComponent2>(new SparseQueryComponent2("test2"));
        
        var query = world.CreateQuery()
            .With<SparseQueryComponent>()
            .With<SparseQueryComponent2>()
            .Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }

        CollectionAssert.Contains(foundEntities, entity1);
        That(foundEntities, Does.Not.Contain(entity2));
        That(foundEntities, Does.Not.Contain(entity3));
        That(foundEntities.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Query_WithMixedArchetypeAndSparseComponents_FindsMatchingEntities()
    {
        using World world = new();
        var entity1 = world.Create<RegularQueryComponent, SparseQueryComponent>(
            new RegularQueryComponent(1.0f), new SparseQueryComponent(10));
        var entity2 = world.Create<RegularQueryComponent>(new RegularQueryComponent(2.0f));
        var entity3 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        
        var query = world.CreateQuery()
            .With<RegularQueryComponent>()
            .With<SparseQueryComponent>()
            .Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }
        
        CollectionAssert.Contains(foundEntities, entity1);
        That(foundEntities, Does.Not.Contain(entity2));
        That(foundEntities, Does.Not.Contain(entity3));
        That(foundEntities.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Query_EnumerateComponents_SparseComponentsAccessible()
    {
        using World world = new();
        var entity1 = world.Create<SparseQueryComponent>(new SparseQueryComponent(10));
        var entity2 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var values = new List<int>();
        foreach (var componentRef in query.Enumerate<SparseQueryComponent>())
        {
            values.Add(componentRef.Item1.Value.Value);
        }
        
        CollectionAssert.Contains(values, 10);
        CollectionAssert.Contains(values, 20);
        That(values.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void Query_EnumerateWithEntities_SparseComponentsWithEntityData()
    {
        using World world = new();
        var entity1 = world.Create<SparseQueryComponent>(new SparseQueryComponent(10));
        var entity2 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var entityComponentPairs = new List<(Entity, int)>();
        foreach ((var entity, var componentRef) in query.EnumerateWithEntities<SparseQueryComponent>())
        {
            entityComponentPairs.Add((entity, componentRef.Value.Value));
        }
        
        CollectionAssert.Contains(entityComponentPairs, (entity1, 10));
        CollectionAssert.Contains(entityComponentPairs, (entity2, 20));
        That(entityComponentPairs.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void Query_WithoutSparseComponent_ExcludesEntitiesWithComponent()
    {
        using World world = new();
        var entity1 = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        var entity2 = world.Create<RegularQueryComponent, SparseQueryComponent>(
            new RegularQueryComponent(2.0f), new SparseQueryComponent(10));
        
        var query = world.CreateQuery()
            .With<RegularQueryComponent>()
            .Without<SparseQueryComponent>()
            .Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }
        
        CollectionAssert.Contains(foundEntities, entity1);
        That(foundEntities, Does.Not.Contain(entity2));
        That(foundEntities.Count, Is.EqualTo(1));
    }
    
    #endregion
    
    #region Query Update Tests
    
    [Test]
    public void WorldUpdate_QueryWithSparseUpdateComponent_ComponentsUpdated()
    {
        using World world = new();
        var entity1 = world.Create<SparseUpdateQueryComponent>(default);
        var entity2 = world.Create<SparseUpdateQueryComponent>(default);
        var entity3 = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        
        world.Update();
        
        ref var component1 = ref entity1.Get<SparseUpdateQueryComponent>();
        ref var component2 = ref entity2.Get<SparseUpdateQueryComponent>();
        
        That(component1.UpdateCount, Is.EqualTo(1));
        That(component2.UpdateCount, Is.EqualTo(1));
        
        // Verify entity3 without sparse component wasn't affected
        That(entity3.Has<SparseUpdateQueryComponent>(), Is.False);
    }
    
    [Test]
    public void WorldUpdate_MultipleSparseUpdateComponents_AllUpdated()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateQueryComponent>(default);
        
        world.Update();
        world.Update();
        
        ref var component = ref entity.Get<SparseUpdateQueryComponent>();
        That(component.UpdateCount, Is.EqualTo(2));
    }
    
    #endregion
    
    #region Dynamic Query Tests
    
    [Test]
    public void Query_AddSparseComponentAtRuntime_EntityIncludedInQuery()
    {
        using World world = new();
        var entity = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        
        var query = world.CreateQuery()
            .With<RegularQueryComponent>()
            .With<SparseQueryComponent>()
            .Build();
        
        // Initially, entity shouldn't match query
        var foundEntities = new List<Entity>();
        foreach (var e in query.EnumerateWithEntities())
        {
            foundEntities.Add(e);
        }
        That(foundEntities.Count, Is.EqualTo(0));
        
        // Add sparse component
        entity.Add<SparseQueryComponent>(new SparseQueryComponent(42));
        
        // Now entity should match query
        foundEntities.Clear();
        foreach (var e in query.EnumerateWithEntities())
        {
            foundEntities.Add(e);
        }

        CollectionAssert.Contains(foundEntities, entity);
        That(foundEntities.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Query_RemoveSparseComponentAtRuntime_EntityExcludedFromQuery()
    {
        using World world = new();
        var entity = world.Create<RegularQueryComponent, SparseQueryComponent>(
            new RegularQueryComponent(1.0f), new SparseQueryComponent(42));
        
        var query = world.CreateQuery()
            .With<RegularQueryComponent>()
            .With<SparseQueryComponent>()
            .Build();
        
        // Initially, entity should match query
        var foundEntities = new List<Entity>();
        foreach (var e in query.EnumerateWithEntities())
        {
            foundEntities.Add(e);
        }
        CollectionAssert.Contains(foundEntities, entity);
        That(foundEntities.Count, Is.EqualTo(1));
        
        // Remove sparse component
        entity.Remove<SparseQueryComponent>();
        
        // Now entity shouldn't match query
        foundEntities.Clear();
        foreach (var e in query.EnumerateWithEntities())
        {
            foundEntities.Add(e);
        }
        That(foundEntities.Count, Is.EqualTo(0));
    }
    
    #endregion
    
    #region Performance and Edge Cases
    
    [Test]
    public void Query_ManyEntitiesWithSparseComponents_EfficientIteration()
    {
        using World world = new();
        const int entityCount = 1000;
        
        var entities = new List<Entity>();
        for (int i = 0; i < entityCount; i++)
        {
            entities.Add(world.Create<SparseQueryComponent>(new SparseQueryComponent(i)));
        }
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var foundCount = 0;
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundCount++;
        }
        
        That(foundCount, Is.EqualTo(entityCount));
    }
    
    [Test]
    public void Query_EmptyResult_HandledGracefully()
    {
        using World world = new();
        var entity = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var foundCount = 0;
        foreach (var e in query.EnumerateWithEntities())
        {
            foundCount++;
        }
        
        That(foundCount, Is.EqualTo(0));
    }
    
    [Test]
    public void Query_OnlyArchetypeComponents_WorksNormally()
    {
        using World world = new();
        var entity1 = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        var entity2 = world.Create<RegularQueryComponent>(new RegularQueryComponent(2.0f));
        
        var query = world.CreateQuery().With<RegularQueryComponent>().Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }

        CollectionAssert.Contains(foundEntities, entity1);
        CollectionAssert.Contains(foundEntities, entity2);
        That(foundEntities, Has.Count.EqualTo(2));
    }
    
    [Test]
    public void Query_OnlySparseComponents_WorksCorrectly()
    {
        using World world = new();
        var entity1 = world.Create<SparseQueryComponent>(new SparseQueryComponent(10));
        var entity2 = world.Create<SparseQueryComponent>(new SparseQueryComponent(20));
        var entity3 = world.Create<RegularQueryComponent>(new RegularQueryComponent(1.0f));
        
        var query = world.CreateQuery().With<SparseQueryComponent>().Build();
        
        var foundEntities = new List<Entity>();
        foreach (var entity in query.EnumerateWithEntities())
        {
            foundEntities.Add(entity);
        }

        CollectionAssert.Contains(foundEntities, entity1);
        CollectionAssert.Contains(foundEntities, entity2);
        That(foundEntities, Does.Not.Contain(entity3));
        That(foundEntities, Has.Count.EqualTo(2));
    }
    
    #endregion
}
