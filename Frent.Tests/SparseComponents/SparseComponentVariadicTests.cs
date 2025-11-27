using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseComponentVariadicTests
{
    #region Test Components
    
    internal struct SparseVariadicComponent1 : ISparseComponent, IUpdate<RegularComponent1>
    {
        public int UpdateCount;
        public int LastValue1;
        
        public void Update(ref RegularComponent1 component)
        {
            UpdateCount++;
            LastValue1 = component.Value;
        }
    }
        
    internal struct SparseVariadicComponent2 : ISparseComponent, IUpdate<RegularComponent1, RegularComponent2>
    {
        public int UpdateCount;
        public int LastValue1;
        public float LastValue2;
        
        public void Update(ref RegularComponent1 component1, ref RegularComponent2 component2)
        {
            UpdateCount++;
            LastValue1 = component1.Value;
            LastValue2 = component2.Value;
        }
    }
    
    internal struct SparseEntityVariadicComponent1 : ISparseComponent, IEntityUpdate<RegularComponent1>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastValue1;
        
        public void Update(Entity self, ref RegularComponent1 component)
        {
            UpdateCount++;
            LastEntity = self;
            LastValue1 = component.Value;
        }
    }
    
    internal struct SparseEntityVariadicComponent2 : ISparseComponent, IEntityUpdate<RegularComponent1, RegularComponent2>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastValue1;
        public float LastValue2;
        
        public void Update(Entity self, ref RegularComponent1 component1, ref RegularComponent2 component2)
        {
            UpdateCount++;
            LastEntity = self;
            LastValue1 = component1.Value;
            LastValue2 = component2.Value;
        }
    }
    
    internal struct SparseUniformVariadicComponent1 : ISparseComponent, IUniformUpdate<TestUniform, RegularComponent1>
    {
        public int UpdateCount;
        public int LastUniformValue;
        public int LastValue1;
        
        public void Update(TestUniform uniform, ref RegularComponent1 component)
        {
            UpdateCount++;
            LastUniformValue = uniform.Value;
            LastValue1 = component.Value;
        }
    }
    
    internal struct SparseUniformVariadicComponent2 : ISparseComponent, IUniformUpdate<TestUniform, RegularComponent1, RegularComponent2>
    {
        public int UpdateCount;
        public int LastUniformValue;
        public int LastValue1;
        public float LastValue2;
        
        public void Update(TestUniform uniform, ref RegularComponent1 component1, ref RegularComponent2 component2)
        {
            UpdateCount++;
            LastUniformValue = uniform.Value;
            LastValue1 = component1.Value;
            LastValue2 = component2.Value;
        }
    }
    
    internal struct SparseEntityUniformVariadicComponent1 : ISparseComponent, IEntityUniformUpdate<TestUniform, RegularComponent1>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastUniformValue;
        public int LastValue1;
        
        public void Update(Entity self, TestUniform uniform, ref RegularComponent1 component)
        {
            UpdateCount++;
            LastEntity = self;
            LastUniformValue = uniform.Value;
            LastValue1 = component.Value;
        }
    }
    
    internal struct SparseEntityUniformVariadicComponent2 : ISparseComponent, IEntityUniformUpdate<TestUniform, RegularComponent1, RegularComponent2>
    {
        public int UpdateCount;
        public Entity LastEntity;
        public int LastUniformValue;
        public int LastValue1;
        public float LastValue2;
        
        public void Update(Entity self, TestUniform uniform, ref RegularComponent1 component1, ref RegularComponent2 component2)
        {
            UpdateCount++;
            LastEntity = self;
            LastUniformValue = uniform.Value;
            LastValue1 = component1.Value;
            LastValue2 = component2.Value;
        }
    }
    
    internal struct RegularComponent1
    {
        public int Value;
        
        public RegularComponent1(int value)
        {
            Value = value;
        }
    }
    
    internal struct RegularComponent2
    {
        public float Value;
        
        public RegularComponent2(float value)
        {
            Value = value;
        }
    }
    
    internal struct TestUniform
    {
        public int Value;
        
        public TestUniform(int value)
        {
            Value = value;
        }
    }
    
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
    
    #region Single Component Reference Tests
    
    [Test]
    public void WorldUpdate_SparseComponentWithSingleComponentReference_UpdateCalledCorrectly()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, SparseVariadicComponent1>(
            new RegularComponent1(42), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseVariadicComponent1>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastValue1, Is.EqualTo(42));
    }
    
    [Test]
    public void WorldUpdate_SparseEntityComponentWithSingleComponentReference_UpdateCalledWithEntity()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, SparseEntityVariadicComponent1>(
            new RegularComponent1(42), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityVariadicComponent1>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastValue1, Is.EqualTo(42));
    }
    
    [Test]
    public void WorldUpdate_SparseUniformComponentWithSingleComponentReference_UpdateCalledWithUniform()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(100) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent1, SparseUniformVariadicComponent1>(
            new RegularComponent1(42), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseUniformVariadicComponent1>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastUniformValue, Is.EqualTo(100));
        That(component.LastValue1, Is.EqualTo(42));
    }
    
    [Test]
    public void WorldUpdate_SparseEntityUniformComponentWithSingleComponentReference_UpdateCalledWithAll()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(100) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent1, SparseEntityUniformVariadicComponent1>(
            new RegularComponent1(42), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityUniformVariadicComponent1>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastUniformValue, Is.EqualTo(100));
        That(component.LastValue1, Is.EqualTo(42));
    }
    
    #endregion
    
    #region Multiple Component Reference Tests
    
    [Test]
    public void WorldUpdate_SparseComponentWithMultipleComponentReferences_UpdateCalledCorrectly()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, RegularComponent2, SparseVariadicComponent2>(
            new RegularComponent1(42), new RegularComponent2(3.14f), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseVariadicComponent2>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastValue1, Is.EqualTo(42));
        That(component.LastValue2, Is.EqualTo(3.14f));
    }
    
    [Test]
    public void WorldUpdate_SparseEntityComponentWithMultipleComponentReferences_UpdateCalledWithEntity()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, RegularComponent2, SparseEntityVariadicComponent2>(
            new RegularComponent1(42), new RegularComponent2(3.14f), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityVariadicComponent2>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastValue1, Is.EqualTo(42));
        That(component.LastValue2, Is.EqualTo(3.14f).Within(0.001f));
    }
    
    [Test]
    public void WorldUpdate_SparseUniformComponentWithMultipleComponentReferences_UpdateCalledWithUniform()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(100) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent1, RegularComponent2, SparseUniformVariadicComponent2>(
            new RegularComponent1(42), new RegularComponent2(3.14f), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseUniformVariadicComponent2>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastUniformValue, Is.EqualTo(100));
        That(component.LastValue1, Is.EqualTo(42));
        That(component.LastValue2, Is.EqualTo(3.14f).Within(0.001f));
    }
    
    [Test]
    public void WorldUpdate_SparseEntityUniformComponentWithMultipleComponentReferences_UpdateCalledWithAll()
    {
        var uniformProvider = new TestUniformProvider { TestUniform = new TestUniform(100) };
        using World world = new(uniformProvider);
        var entity = world.Create<RegularComponent1, RegularComponent2, SparseEntityUniformVariadicComponent2>(
            new RegularComponent1(42), new RegularComponent2(3.14f), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseEntityUniformVariadicComponent2>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastEntity, Is.EqualTo(entity));
        That(component.LastUniformValue, Is.EqualTo(100));
        That(component.LastValue1, Is.EqualTo(42));
        That(component.LastValue2, Is.EqualTo(3.14f).Within(0.001f));
    }
    
    #endregion
    
    #region Missing Component Tests
    
    [Test]
    public void WorldUpdate_SparseComponentMissingRequiredComponent_UpdateNotCalled()
    {
        using World world = new();
        var entity = world.Create<SparseVariadicComponent1>(default);

        MissingComponentException? e = Throws<MissingComponentException>(world.Update);

        That(e, Is.Not.Null);
        That(e!.InvalidEntity, Is.EqualTo(entity));
        That(e!.ComponentType, Is.EqualTo(typeof(SparseVariadicComponent1)));
        That(e!.MissingComponent, Is.EqualTo(typeof(RegularComponent1)));
    }
    
    [Test]
    public void WorldUpdate_SparseComponentMissingOneOfMultipleRequiredComponents_UpdateNotCalled()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, SparseVariadicComponent2>(
            new RegularComponent1(42), default);

        MissingComponentException? e = Throws<MissingComponentException>(world.Update);

        That(e, Is.Not.Null);
        That(e!.InvalidEntity, Is.EqualTo(entity));
        That(e!.ComponentType, Is.EqualTo(typeof(SparseVariadicComponent2)));
        That(e!.MissingComponent, Is.EqualTo(typeof(RegularComponent2)));
    }

    #endregion

    #region Component Modification During Update Tests

    [Test]
    public void WorldUpdate_SparseComponentModifiesReferencedComponent_ChangesVisible()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, SparseVariadicComponent1>(
            new RegularComponent1(42), default);
        
        world.Update();
        
        ref var component = ref entity.Get<SparseVariadicComponent1>();
        That(component.UpdateCount, Is.EqualTo(1));
        That(component.LastValue1, Is.EqualTo(42));
        
        // Modify the referenced component
        ref var regularComponent = ref entity.Get<RegularComponent1>();
        regularComponent.Value = 100;
        
        world.Update();
        
        That(component.UpdateCount, Is.EqualTo(2));
        That(component.LastValue1, Is.EqualTo(100));
    }
    
    #endregion
    
    #region Mixed Sparse and Regular Component Updates
    
    [Test]
    public void WorldUpdate_MixedSparseAndRegularVariadicComponents_BothUpdateCorrectly()
    {
        using World world = new();
        
        // Create a regular component with variadic update for comparison
        var regularUpdateComponent = new RegularVariadicUpdateComponent();
        
        var entity = world.Create<RegularComponent1, SparseVariadicComponent1>(
            new RegularComponent1(42), default);
        
        entity.Add(regularUpdateComponent);
        
        world.Update();
        
        ref var sparseComponent = ref entity.Get<SparseVariadicComponent1>();
        ref var regularComponent = ref entity.Get<RegularVariadicUpdateComponent>();
        
        That(sparseComponent.UpdateCount, Is.EqualTo(1));
        That(sparseComponent.LastValue1, Is.EqualTo(42));
        That(regularComponent.UpdateCount, Is.EqualTo(1));
    }
    
    internal struct RegularVariadicUpdateComponent : IUpdate
    {
        public int UpdateCount;
        
        public void Update()
        {
            UpdateCount++;
        }
    }
    
    #endregion
    
    #region Edge Cases
    
    [Test]
    public void WorldUpdate_SparseComponentReferencingItself_HandledCorrectly()
    {
        using World world = new();
        var entity = world.Create<SparseVariadicComponent1, SparseVariadicSelfReferenceComponent>(default, default);
        
        // Add the required component
        entity.Add(new RegularComponent1(42));
        
        world.Update();
        
        ref var component = ref entity.Get<SparseVariadicComponent1>();
        ref var selfRefComponent = ref entity.Get<SparseVariadicSelfReferenceComponent>();
        
        That(component.UpdateCount, Is.EqualTo(1));
        That(selfRefComponent.UpdateCount, Is.EqualTo(1));
    }
    
    internal struct SparseVariadicSelfReferenceComponent : ISparseComponent, IUpdate<SparseVariadicSelfReferenceComponent>
    {
        public int UpdateCount;
        
        public void Update(ref SparseVariadicSelfReferenceComponent self)
        {
            UpdateCount++;
        }
    }
    
    [Test]
    public void WorldUpdate_MultipleSparseVariadicComponentsOnSameEntity_AllUpdateCorrectly()
    {
        using World world = new();
        var entity = world.Create<RegularComponent1, RegularComponent2, SparseVariadicComponent1, SparseVariadicComponent2>(
            new RegularComponent1(42), new RegularComponent2(3.14f), default, default);
        
        world.Update();
        
        ref var component1 = ref entity.Get<SparseVariadicComponent1>();
        ref var component2 = ref entity.Get<SparseVariadicComponent2>();
        
        That(component1.UpdateCount, Is.EqualTo(1));
        That(component1.LastValue1, Is.EqualTo(42));
        
        That(component2.UpdateCount, Is.EqualTo(1));
        That(component2.LastValue1, Is.EqualTo(42));
        That(component2.LastValue2, Is.EqualTo(3.14f));
    }
    
    #endregion
}
