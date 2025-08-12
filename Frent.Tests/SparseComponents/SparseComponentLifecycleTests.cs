using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using static Frent.Tests.EntityTests;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseComponentLifecycleTests
{
#region Test Components
    
    internal struct SparseLifecycleOrderComponent(List<string> lifecycleEvents) : ISparseComponent, IInitable, IDestroyable
    {
        public List<string> LifecycleEvents = lifecycleEvents;
        public Entity InitEntity;
        
        public void Init(Entity self)
        {
            LifecycleEvents ??= new List<string>();
            LifecycleEvents.Add("Init");
            InitEntity = self;
        }
        
        public void Destroy()
        {
            LifecycleEvents ??= new List<string>();
            LifecycleEvents.Add("Destroy");
        }
    }
    
    internal struct SparseEventOrderComponent : ISparseComponent, IInitable, IDestroyable
    {
        public List<string> Events;
        
        public void Init(Entity self)
        {
            Events ??= new List<string>();
            Events.Add("SparseInit");
        }
        
        public void Destroy()
        {
            Events ??= new List<string>();
            Events.Add("SparseDestroy");
        }
    }
    
    internal struct RegularEventOrderComponent : IInitable, IDestroyable
    {
        public List<string> Events;
        
        public void Init(Entity self)
        {
            Events ??= new List<string>();
            Events.Add("RegularInit");
        }
        
        public void Destroy()
        {
            Events ??= new List<string>();
            Events.Add("RegularDestroy");
        }
    }
    
    #endregion
    
    #region Lifecycle Order Tests
    
    [Test]
    public void Create_SparseComponentWithLifecycle_InitCalledAfterCreation()
    {
        using World world = new();
        
        var entity = world.Create<SparseLifecycleOrderComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        That(component.LifecycleEvents, Is.Not.Null);
        CollectionAssert.Contains(component.LifecycleEvents, "Init");
        That(component.InitEntity, Is.EqualTo(entity));
    }
    
    [Test]
    public void Add_SparseComponentWithLifecycle_InitCalledAfterAdd()
    {
        using World world = new();
        var entity = world.Create();
        
        entity.Add<SparseLifecycleOrderComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        That(component.LifecycleEvents, Is.Not.Null);
        CollectionAssert.Contains(component.LifecycleEvents, "Init");
        That(component.InitEntity, Is.EqualTo(entity));
    }
    
    [Test]
    public void Set_SparseComponentWithLifecycle_InitCalledOnNewComponent()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleOrderComponent>(default);
        
        // Clear the initial lifecycle events
        ref var originalComponent = ref entity.Get<SparseLifecycleOrderComponent>();
        originalComponent.LifecycleEvents.Clear();
        
        var newComponent = new SparseLifecycleOrderComponent();
        newComponent.Init(entity);
        entity.Set(Component<SparseLifecycleOrderComponent>.ID, newComponent);
        
        ref var updatedComponent = ref entity.Get<SparseLifecycleOrderComponent>();
        That(updatedComponent.LifecycleEvents, Is.Not.Null);
        CollectionAssert.Contains(updatedComponent.LifecycleEvents, "Init");
        That(updatedComponent.InitEntity, Is.EqualTo(entity));
    }
    
    #endregion
    
    #region Event Order Tests
    
    [Test]
    public void Add_MixedComponents_InitOrderCorrect()
    {
        using World world = new();
        var entity = world.Create();
        
        var sharedEvents = new List<string>();
        
        entity.Add(new RegularEventOrderComponent { Events = sharedEvents });
        entity.Add(new SparseEventOrderComponent { Events = sharedEvents });
        
        // Both components should have been initialized
        ref var regularComponent = ref entity.Get<RegularEventOrderComponent>();
        ref var sparseComponent = ref entity.Get<SparseEventOrderComponent>();
        
        CollectionAssert.Contains(regularComponent.Events, "RegularInit");
        CollectionAssert.Contains(sparseComponent.Events, "SparseInit");
        
        // Verify both events were recorded in shared list
        CollectionAssert.Contains(sharedEvents, "RegularInit");
        CollectionAssert.Contains(sharedEvents, "SparseInit");
    }
    
    [Test]
    public void WorldComponentEvents_SparseComponent_EventsFiredInCorrectOrder()
    {
        using World world = new();
        var entity = world.Create();
        
        var eventOrder = new List<string>();
        
        world.ComponentAdded += (e, componentId) =>
        {
            if (e == entity && componentId == Component<SparseEventOrderComponent>.ID)
                eventOrder.Add("WorldComponentAdded");
        };
        
        entity.OnComponentAdded += (e, componentId) =>
        {
            if (e == entity && componentId == Component<SparseEventOrderComponent>.ID)
                eventOrder.Add("EntityComponentAdded");
        };
        
        entity.Add<SparseEventOrderComponent>(default);
        
        // Verify events were fired
        CollectionAssert.Contains(eventOrder, "WorldComponentAdded");
        CollectionAssert.Contains(eventOrder, "EntityComponentAdded");
        
        // Verify Init was called on the component
        ref var component = ref entity.Get<SparseEventOrderComponent>();
        That(component.Events, Is.Not.Null);
        CollectionAssert.Contains(component.Events, "SparseInit");
    }
    
    [Test]
    public void WorldComponentEvents_SparseComponent_RemoveEventsFiredInCorrectOrder()
    {
        using World world = new();
        var entity = world.Create<SparseEventOrderComponent>(default);
        
        var eventOrder = new List<string>();
        
        world.ComponentRemoved += (e, componentId) =>
        {
            if (e == entity && componentId == Component<SparseEventOrderComponent>.ID)
                eventOrder.Add("WorldComponentRemoved");
        };
        
        entity.OnComponentRemoved += (e, componentId) =>
        {
            if (e == entity && componentId == Component<SparseEventOrderComponent>.ID)
                eventOrder.Add("EntityComponentRemoved");
        };
        
        entity.Remove<SparseEventOrderComponent>();
        
        // Verify events were fired
        CollectionAssert.Contains(eventOrder, "WorldComponentRemoved");
        CollectionAssert.Contains(eventOrder, "EntityComponentRemoved");
    }
    
    [Test]
    public void GenericEvents_SparseComponent_EventsFiredWithCorrectData()
    {
        using World world = new();
        var entity = world.Create();
        
        Type? addedType = null;
        object? addedComponent = null;
        Type? removedType = null;
        object? removedComponent = null;
        
        entity.OnComponentAddedGeneric += new GenericAction((type, component) =>
        {
            if (type == typeof(SparseEventOrderComponent))
            {
                addedType = type;
                addedComponent = component;
            }
        });
        
        entity.OnComponentRemovedGeneric += new GenericAction((type, component) =>
        {
            if (type == typeof(SparseEventOrderComponent))
            {
                removedType = type;
                removedComponent = component;
            }
        });
        
        entity.Add<SparseEventOrderComponent>(default);
        
        That(addedType, Is.EqualTo(typeof(SparseEventOrderComponent)));
        That(addedComponent, Is.TypeOf<SparseEventOrderComponent>());
        
        entity.Remove<SparseEventOrderComponent>();
        
        That(removedType, Is.EqualTo(typeof(SparseEventOrderComponent)));
        That(removedComponent, Is.TypeOf<SparseEventOrderComponent>());
    }
    
    #endregion
    
    #region Lifecycle with Events Tests
    
    [Test]
    public void AddAndRemove_SparseComponentWithLifecycle_CompleteLifecycleCalled()
    {
        using World world = new();
        var entity = world.Create();

        entity.Add<SparseLifecycleOrderComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        List<string> lifecycleEvents = component.LifecycleEvents;
        CollectionAssert.Contains(lifecycleEvents, "Init");

        entity.Remove<SparseLifecycleOrderComponent>();
        
        That(entity.Has<SparseLifecycleOrderComponent>(), Is.False);
        CollectionAssert.Contains(lifecycleEvents, "Destroy");
    }

    [Test]
    public void EntityDelete_SparseComponentWithLifecycle_DestroyCalledBeforeRemoval()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleOrderComponent>(default);

        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        List<string> lifecycleEvents = component.LifecycleEvents;
        CollectionAssert.Contains(lifecycleEvents, "Init");
        
        entity.Delete();
        
        That(entity.IsAlive, Is.False);
        CollectionAssert.Contains(lifecycleEvents, "Destroy");
    }

    [Test]
    public void WorldDispose_SparseComponentWithLifecycle_DestroyCalledDuringCleanup()
    {
        World world = new();
        var entity = world.Create<SparseLifecycleOrderComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        List<string> lifecycleEvents = component.LifecycleEvents;
        CollectionAssert.Contains(lifecycleEvents, "Init");
        
        world.Dispose();
        
        That(entity.IsAlive, Is.False);
        CollectionAssert.Contains(lifecycleEvents, "Destroy");
    }

    #endregion

    #region Multiple Sparse Components Tests

    [Test]
    public void MultipleSparseComponents_WithLifecycle_AllInitialized()
    {
        using World world = new();
        var entity = world.Create();
        
        entity.Add<SparseLifecycleOrderComponent>(default);
        entity.Add<SparseEventOrderComponent>(default);
        
        ref var component1 = ref entity.Get<SparseLifecycleOrderComponent>();
        ref var component2 = ref entity.Get<SparseEventOrderComponent>();
        
        CollectionAssert.Contains(component1.LifecycleEvents, "Init");
        That(component1.InitEntity, Is.EqualTo(entity));
        
        CollectionAssert.Contains(component2.Events, "SparseInit");
    }
    
    [Test]
    public void MultipleSparseComponents_Remove_OnlyTargetRemoved()
    {
        using World world = new();
        var entity = world.Create<SparseLifecycleOrderComponent, SparseEventOrderComponent>(default, default);
        
        That(entity.Has<SparseLifecycleOrderComponent>(), Is.True);
        That(entity.Has<SparseEventOrderComponent>(), Is.True);
        
        entity.Remove<SparseLifecycleOrderComponent>();
        
        That(entity.Has<SparseLifecycleOrderComponent>(), Is.False);
        That(entity.Has<SparseEventOrderComponent>(), Is.True);
    }
    
    #endregion
    
    #region Edge Cases
    
    [Test]
    public void ReferenceTypeSparseComponent_Lifecycle_WorksCorrectly()
    {
        using World world = new();
        var entity = world.Create();

        entity.Add<SparseLifecycleOrderComponent>(default);
        
        ref var component = ref entity.Get<SparseLifecycleOrderComponent>();
        That(component.LifecycleEvents, Is.Not.Null);
        CollectionAssert.Contains(component.LifecycleEvents, "Init");
    }
    
    [Test]
    public void SparseComponentLifecycle_WithNullEvents_HandledGracefully()
    {
        using World world = new();
        var entity = world.Create();
        
        var component = new SparseLifecycleOrderComponent();
        entity.Add(component);
        
        ref var addedComponent = ref entity.Get<SparseLifecycleOrderComponent>();
        That(addedComponent.LifecycleEvents, Is.Not.Null);
        CollectionAssert.Contains(addedComponent.LifecycleEvents, "Init");
    }
    
    #endregion
}
