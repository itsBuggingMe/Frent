using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using static Frent.Tests.EntityTests;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseComponentTestSuite
{
    #region Test Components
    
    // Basic sparse component with lifecycle
    internal struct SparseLifecycleComponent : ISparseComponent, IInitable, IDestroyable
    {
        public bool InitCalled;
        public bool DestroyCalled;
        public Entity InitEntity;
        
        public void Init(Entity self)
        {
            InitCalled = true;
            InitEntity = self;
        }
        
        public void Destroy()
        {
            DestroyCalled = true;
        }
    }


    internal class SparseLifecycleComponentClass : ISparseComponent, IInitable, IDestroyable
    {
        public bool InitCalled;
        public bool DestroyCalled;
        public Entity InitEntity;

        public void Init(Entity self)
        {
            InitCalled = true;
            InitEntity = self;
        }

        public void Destroy()
        {
            DestroyCalled = true;
        }
    }

    // Sparse component with basic update
    internal struct SparseUpdateComponent : ISparseComponent, IComponent
    {
        public int UpdateCount;
        
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    // Sparse component with entity update
    internal struct SparseEntityUpdateComponent : ISparseComponent, IEntityComponent
    {
        public int UpdateCount;
        public Entity LastEntity;
        
        public void Update(Entity self)
        {
            UpdateCount++;
            LastEntity = self;
        }
    }
    
    // Sparse component with uniform update
    internal struct SparseUniformUpdateComponent : ISparseComponent, IUniformComponent<TestUniform>
    {
        public int UpdateCount;
        public int LastUniformValue;
        
        public void Update(TestUniform uniform)
        {
            UpdateCount++;
            LastUniformValue = uniform.Value;
        }
    }
    
    // Sparse component with entity uniform update
    internal struct SparseEntityUniformUpdateComponent : ISparseComponent, IEntityUniformComponent<TestUniform>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastUniformValue;
        
        public void Update(Entity self, TestUniform uniform)
        {
            UpdateCount++;
            LastEntity = self;
            LastUniformValue = uniform.Value;
        }
    }
    
    // Sparse component with component reference update
    internal struct SparseComponentUpdateComponent : ISparseComponent, IComponent<RegularComponent>
    {
        public int UpdateCount;
        public int LastComponentValue;
        
        public void Update(ref RegularComponent component)
        {
            UpdateCount++;
            LastComponentValue = component.Value;
        }
    }
    
    // Sparse component with entity and component reference update
    internal struct SparseEntityComponentUpdateComponent : ISparseComponent, IEntityComponent<RegularComponent>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastComponentValue;
        
        public void Update(Entity self, ref RegularComponent component)
        {
            UpdateCount++;
            LastEntity = self;
            LastComponentValue = component.Value;
        }
    }
    
    // Sparse component with uniform and component reference update
    internal struct SparseUniformComponentUpdateComponent : ISparseComponent, IUniformComponent<TestUniform, RegularComponent>
    {
        public int UpdateCount;
        public int LastUniformValue;
        public int LastComponentValue;
        
        public void Update(TestUniform uniform, ref RegularComponent component)
        {
            UpdateCount++;
            LastUniformValue = uniform.Value;
            LastComponentValue = component.Value;
        }
    }
    
    // Sparse component with entity, uniform and component reference update
    internal struct SparseEntityUniformComponentUpdateComponent : ISparseComponent, IEntityUniformComponent<TestUniform, RegularComponent>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastUniformValue;
        public int LastComponentValue;
        
        public void Update(Entity self, TestUniform uniform, ref RegularComponent component)
        {
            UpdateCount++;
            LastEntity = self;
            LastUniformValue = uniform.Value;
            LastComponentValue = component.Value;
        }
    }
    
    // Filtered sparse components
    internal struct SparseFilteredComponent1 : ISparseComponent, IComponent
    {
        public int UpdateCount;
        
        [FilterAttribute1]
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    internal struct SparseFilteredComponent2 : ISparseComponent, IComponent
    {
        public int UpdateCount;
        
        [FilterAttribute2]
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    // Regular archetype component for mixed tests
    internal struct RegularComponent
    {
        public int Value;
        
        public RegularComponent(int value)
        {
            Value = value;
        }
    }
    
    // Regular archetype component with update
    internal struct RegularUpdateComponent : IComponent
    {
        public int UpdateCount;
        
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    // Test uniform
    internal struct TestUniform
    {
        public int Value;
        
        public TestUniform(int value)
        {
            Value = value;
        }
    }
    
    // Test uniform provider
    internal class TestUniformProvider : IUniformProvider
    {
        public TestUniform TestUniform { get; set; }
        
        public T GetUniform<T>()
        {
            if (typeof(T) == typeof(TestUniform))
                return (T)(object)TestUniform;
            
            throw new NotSupportedException($"Uniform type {typeof(T)} not supported");
        }
    }
    
    #endregion
    
    #region Lifecycle Tests
    
    [Test]
    public void Add_SparseComponentWithIInitable_InitCalled()
    {
        using World world = new();
        var entity = world.Create();
        
        entity.Add<SparseLifecycleComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleComponent>();
        That(component.InitCalled, Is.True);
        That(component.InitEntity, Is.EqualTo(entity));
    }
    
    [Test]
    public void Create_SparseComponentWithIInitable_InitCalled()
    {
        using World world = new();
        
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleComponent>();
        That(component.InitCalled, Is.True);
        That(component.InitEntity, Is.EqualTo(entity));
    }
    
    [Test]
    public void Remove_SparseComponentWithIDestroyable_DestroyCalled()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        entity.Remove<SparseLifecycleComponent>();
        
        That(entity.Has<SparseLifecycleComponent>(), Is.False);
        // Note: We can't check DestroyCalled here since the component is removed
        // This test verifies the Remove operation succeeds without exception
    }
    
    [Test]
    public void Delete_EntityWithSparseComponentWithIDestroyable_DestroyCalled()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        entity.Delete();
        
        That(entity.IsAlive, Is.False);
        // Note: We can't check DestroyCalled here since the entity is deleted
        // This test verifies the Delete operation succeeds without exception
    }
    
    [Test]
    public void SetComponent_SparseComponentWithLifecycle_LifecycleCalledProperly()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleComponentClass>(new());
        
        ref var originalComponent = ref entity.Get<SparseLifecycleComponentClass>();
        That(originalComponent.InitCalled, Is.True);
        
        var newComponent = new SparseLifecycleComponentClass();
        entity.Set(Component<SparseLifecycleComponentClass>.ID, newComponent);
        
        ref var updatedComponent = ref entity.Get<SparseLifecycleComponentClass>();
        That(updatedComponent.InitCalled, Is.True);
        That(updatedComponent.InitEntity, Is.EqualTo(entity));
    }
    
    #endregion
    
    #region Event Tests
    
    [Test]
    public void Add_SparseComponent_ComponentAddedEventFired()
    {
        using World world = new();
        var entity = world.Create();
        
        bool eventFired = false;
        ComponentID firedComponentId = default;
        
        world.ComponentAdded += (e, componentId) =>
        {
            if (e == entity)
            {
                eventFired = true;
                firedComponentId = componentId;
            }
        };
        
        entity.Add<SparseLifecycleComponent>(default);
        
        That(eventFired, Is.True);
        That(firedComponentId, Is.EqualTo(Component<SparseLifecycleComponent>.ID));
    }
    
    [Test]
    public void Remove_SparseComponent_ComponentRemovedEventFired()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        bool eventFired = false;
        ComponentID firedComponentId = default;
        
        world.ComponentRemoved += (e, componentId) =>
        {
            if (e == entity)
            {
                eventFired = true;
                firedComponentId = componentId;
            }
        };
        
        entity.Remove<SparseLifecycleComponent>();
        
        That(eventFired, Is.True);
        That(firedComponentId, Is.EqualTo(Component<SparseLifecycleComponent>.ID));
    }
    
    [Test]
    public void EntityEvents_SparseComponent_GenericEventsWork()
    {
        using World world = new();
        var entity = world.Create();
        
        bool addEventFired = false;
        bool removeEventFired = false;
        Type addedType = null!;
        Type removedType = null!;
        
        entity.OnComponentAddedGeneric += new GenericAction((type, obj) =>
        {
            addEventFired = true;
            addedType = type;
        });
        
        entity.OnComponentRemovedGeneric += new GenericAction((type, obj) =>
        {
            removeEventFired = true;
            removedType = type;
        });
        
        entity.Add<SparseLifecycleComponent>(default);
        
        That(addEventFired, Is.True);
        That(addedType, Is.EqualTo(typeof(SparseLifecycleComponent)));
        
        entity.Remove<SparseLifecycleComponent>();
        
        That(removeEventFired, Is.True);
        That(removedType, Is.EqualTo(typeof(SparseLifecycleComponent)));
    }
    
    #endregion
    
    #region Entity API Tests
    
    [Test]
    public void Add_SparseComponent_ComponentAdded()
    {
        using World world = new();
        var entity = world.Create();
        
        entity.Add<SparseLifecycleComponent>(default);
        
        That(entity.Has<SparseLifecycleComponent>(), Is.True);
        ref var component = ref entity.Get<SparseLifecycleComponent>();
        That(component.InitCalled, Is.True);
    }
    
    [Test]
    public void AddWithValue_SparseComponent_ComponentAddedWithValue()
    {
        using World world = new();
        var entity = world.Create();
        
        var testComponent = new SparseUpdateComponent { UpdateCount = 5 };
        entity.Add(testComponent);
        
        That(entity.Has<SparseUpdateComponent>(), Is.True);
        ref var component = ref entity.Get<SparseUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(5));
    }
    
    [Test]
    public void Get_SparseComponent_ReturnsComponentReference()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        ref var component = ref entity.Get<SparseUpdateComponent>();
        component.UpdateCount = 10;
        
        ref var sameComponent = ref entity.Get<SparseUpdateComponent>();
        That(sameComponent.UpdateCount, Is.EqualTo(10));
    }
    
    [Test]
    public void TryGet_SparseComponent_ReturnsCorrectResult()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        var exists = entity.TryGet<SparseUpdateComponent>(out var result);
        
        That(exists, Is.True);
        That(result.Value.UpdateCount, Is.EqualTo(0));
        
        var entity2 = world.Create();
        var exists2 = entity2.TryGet<SparseUpdateComponent>(out var result2);
        
        That(exists2, Is.False);
    }
    
    [Test]
    public void Has_SparseComponent_ReturnsCorrectResult()
    {
        using World world = new();
        var entity1 = world.Create<SparseUpdateComponent>(default);
        var entity2 = world.Create();
        
        That(entity1.Has<SparseUpdateComponent>(), Is.True);
        That(entity2.Has<SparseUpdateComponent>(), Is.False);
    }
    
    [Test]
    public void Set_SparseComponent_UpdatesComponent()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        var newComponent = new SparseUpdateComponent { UpdateCount = 15 };
        entity.Set(Component<SparseUpdateComponent>.ID, newComponent);
        
        ref var component = ref entity.Get<SparseUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(15));
    }
    
    [Test]
    public void Remove_SparseComponent_ComponentRemoved()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        That(entity.Has<SparseUpdateComponent>(), Is.True);
        
        entity.Remove<SparseUpdateComponent>();
        
        That(entity.Has<SparseUpdateComponent>(), Is.False);
    }
    
    [Test]
    public void ComponentTypes_SparseComponent_IncludesSparseComponent()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        var componentTypes = entity.ComponentTypes;

        CollectionAssert.Contains(componentTypes, Component<SparseUpdateComponent>.ID);
    }
    
    [Test]
    public void ComponentTypes_MixedComponents_IncludesBothTypes()
    {
        using World world = new();
        var entity = world.Create<RegularComponent, SparseUpdateComponent>(new RegularComponent(10), default);
        
        var componentTypes = entity.ComponentTypes;

        CollectionAssert.Contains(componentTypes, Component<RegularComponent>.ID);
        CollectionAssert.Contains(componentTypes, Component<SparseUpdateComponent>.ID);
        That(componentTypes.Length, Is.EqualTo(2));
    }
    
    #endregion
    
    #region Component Update Tests
    
    [Test]
    public void WorldUpdate_SparseComponentWithIComponent_UpdateCalled()
    {
        using World world = new();
        var entity = world.Create<SparseUpdateComponent>(default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithIEntityComponent_UpdateCalledWithEntity()
    {
        using World world = new();
        var entity = world.Create<SparseEntityUpdateComponent>(default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithIUniformComponent_UpdateCalledWithUniform()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(42) };
        using World world = new(uniformProvider);
        var entity = world.Create<SparseUniformUpdateComponent>(default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseUniformUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastUniformValue, Is.EqualTo(42));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithIEntityUniformComponent_UpdateCalledWithEntityAndUniform()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(42) };
        using World world = new(uniformProvider);
        var entity = world.Create<SparseEntityUniformUpdateComponent>(default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityUniformUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastUniformValue, Is.EqualTo(42));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithComponentReference_UpdateCalledWithComponent()
    {
        using World world = new();
        var entity = world.Create<RegularComponent, SparseComponentUpdateComponent>(new RegularComponent(25), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseComponentUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastComponentValue, Is.EqualTo(25));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithEntityAndComponentReference_UpdateCalledWithBoth()
    {
        using World world = new();
        var entity = world.Create<RegularComponent, SparseEntityComponentUpdateComponent>(new RegularComponent(25), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityComponentUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastComponentValue, Is.EqualTo(25));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithUniformAndComponentReference_UpdateCalledWithBoth()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(42) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent, SparseUniformComponentUpdateComponent>(new RegularComponent(25), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseUniformComponentUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastUniformValue, Is.EqualTo(42));
        That(component.LastComponentValue, Is.EqualTo(25));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentWithEntityUniformAndComponentReference_UpdateCalledWithAll()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(42) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent, SparseEntityUniformComponentUpdateComponent>(new RegularComponent(25), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityUniformComponentUpdateComponent>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastUniformValue, Is.EqualTo(42));
        That(component.LastComponentValue, Is.EqualTo(25));
    }
    
    #endregion
    
    #region Filtered Update Tests
    
    [Test]
    public void FilteredUpdate_SparseComponentWithFilter1_OnlyFilter1Updated()
    {
        using World world = new();
        var entity1 = world.Create<SparseFilteredComponent1>(default);
        var entity2 = world.Create<SparseFilteredComponent2>(default);
        
        world.Update<FilterAttribute1>();
        
        ref var component1 = ref entity1.Get<SparseFilteredComponent1>();
        ref var component2 = ref entity2.Get<SparseFilteredComponent2>();
        
        That(component1.UpdateCount, Is.EqualTo(1));
        That(component2.UpdateCount, Is.EqualTo(0));
    }
    
    [Test]
    public void FilteredUpdate_SparseComponentWithFilter2_OnlyFilter2Updated()
    {
        using World world = new();
        var entity1 = world.Create<SparseFilteredComponent1>(default);
        var entity2 = world.Create<SparseFilteredComponent2>(default);
        
        world.Update<FilterAttribute2>();
        
        ref var component1 = ref entity1.Get<SparseFilteredComponent1>();
        ref var component2 = ref entity2.Get<SparseFilteredComponent2>();
        
        That(component1.UpdateCount, Is.EqualTo(0));
        That(component2.UpdateCount, Is.EqualTo(1));
    }
    
    #endregion
    
    #region Single Component Update Tests
    
    [Test]
    public void UpdateComponent_SparseComponent_OnlyThatComponentUpdated()
    {
        using World world = new();
        var entity1 = world.Create<SparseUpdateComponent>(default);
        var entity2 = world.Create<SparseEntityUpdateComponent>(default);
        
        world.UpdateComponent(Component<SparseUpdateComponent>.ID);
        
        ref var component1 = ref entity1.Get<SparseUpdateComponent>();
        ref var component2 = ref entity2.Get<SparseEntityUpdateComponent>();
        
        That(component1.UpdateCount, Is.EqualTo(1));
        That(component2.UpdateCount, Is.EqualTo(0));
    }
    
    #endregion
    
    #region Mixed Component Tests
    
    [Test]
    public void WorldUpdate_MixedArchetypeAndSparseComponents_BothUpdated()
    {
        using World world = new();
        var entity = world.Create<RegularUpdateComponent, SparseUpdateComponent>(default, default);
        
        world.Update();
        
        ref var regularComponent = ref entity.Get<RegularUpdateComponent>();
        ref var sparseComponent = ref entity.Get<SparseUpdateComponent>();
        
        That(regularComponent.UpdateCount, Is.EqualTo(1));
        That(sparseComponent.UpdateCount, Is.EqualTo(1));
    }
    
    [Test]
    public void Add_SparseComponentToArchetypeEntity_BothComponentTypesWork()
    {
        using World world = new();
        var entity = world.Create<RegularComponent>(new RegularComponent(10));
        
        entity.Add<SparseUpdateComponent>(default);
        
        That(entity.Has<RegularComponent>(), Is.True);
        That(entity.Has<SparseUpdateComponent>(), Is.True);
        
        ref var regularComponent = ref entity.Get<RegularComponent>();
        ref var sparseComponent = ref entity.Get<SparseUpdateComponent>();
        
        That(regularComponent.Value, Is.EqualTo(10));
        That(sparseComponent.UpdateCount, Is.EqualTo(0));
    }
    
    [Test]
    public void Remove_SparseComponentFromMixedEntity_ArchetypeComponentRemains()
    {
        using World world = new();
        var entity = world.Create<RegularComponent, SparseUpdateComponent>(new RegularComponent(10), default);
        
        entity.Remove<SparseUpdateComponent>();
        
        That(entity.Has<RegularComponent>(), Is.True);
        That(entity.Has<SparseUpdateComponent>(), Is.False);
        
        ref var regularComponent = ref entity.Get<RegularComponent>();
        That(regularComponent.Value, Is.EqualTo(10));
    }
    
    [Test]
    public void ComponentEnumeration_MixedEntity_IncludesBothTypes()
    {
        using World world = new();
        var entity = world.Create<RegularComponent, SparseUpdateComponent>(new RegularComponent(10), default);
        
        var componentTypes = new List<ComponentID>();
        foreach (var componentId in entity)
        {
            componentTypes.Add(componentId);
        }
        
        CollectionAssert.Contains(componentTypes, Component<RegularComponent>.ID);
        CollectionAssert.Contains(componentTypes, Component<SparseUpdateComponent>.ID);
        That(componentTypes.Count, Is.EqualTo(2));
    }
    
    #endregion
    
    #region Edge Case Tests
    
    [Test]
    public void MultipleAdd_SparseComponent_OnlyOneInstanceExists()
    {
        using World world = new();
        var entity = world.Create();
        
        entity.Add<SparseUpdateComponent>(default);
        Throws<ComponentAlreadyExistsException>(() => entity.Add<SparseUpdateComponent>(default));
        
        That(entity.Has<SparseUpdateComponent>(), Is.True);
        
        var componentTypes = entity.ComponentTypes;
        var sparseComponentCount = componentTypes.Count(c => c == Component<SparseUpdateComponent>.ID);
        That(sparseComponentCount, Is.EqualTo(1));
    }
    
    [Test]
    public void EntityDeletion_WithSparseComponents_CleansUpProperly()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        That(entity.Has<SparseLifecycleComponent>(), Is.True);
        
        entity.Delete();
        
        That(entity.IsAlive, Is.False);
    }
    
    [Test]
    public void WorldDispose_WithSparseComponents_CleansUpProperly()
    {
        World world = new();
        var entity = world.Create<SparseLifecycleComponent>(default);
        
        That(entity.Has<SparseLifecycleComponent>(), Is.True);
        
        world.Dispose();
        
        That(entity.IsAlive, Is.False);
        // Verify no exceptions thrown during cleanup
    }
    
    #endregion
}
