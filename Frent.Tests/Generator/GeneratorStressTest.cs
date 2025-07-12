using Frent;
using Frent.Components;
using FilterAttribute1Alias = Frent.Tests.Helpers.FilterAttribute1;
using FilterAttribute2Alias = Frent.Tests.Helpers.FilterAttribute2;
using InterfacePackAlias = InterfacePack;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Generator
{
    internal class GeneratorStressTest
    {
        [Test]
        public void Test()
        {
            using World world = new World();

            var componentInstance = new StressTestOuter<long>.Something.Component<double>();
            
            var entity = world.Create(componentInstance, 0.0);

            That(componentInstance.InitCount, Is.EqualTo(1));

            world.Update();
            That(componentInstance.UpdateCount, Is.EqualTo(1));
            That(componentInstance.Update2Count, Is.EqualTo(1));

            world.Update<FilterAttribute2Alias>();
            That(componentInstance.UpdateCount, Is.EqualTo(1));
            That(componentInstance.Update2Count, Is.EqualTo(2));

            entity.Delete();

            That(componentInstance.DestroyCount, Is.EqualTo(1));
        }
    }
}

// nightmare type for source generator
// 1. nested types [x]
// 2. generic types [x]
// 3. multiple interfaces [x]
// 4. inheritance [x]
// 5. initable, destroyable, multiple update interfaces [x]
// 6. global namespace [x]
// 7. unbound generics [x]
// 8. aliases [x]
// 9. update filtering [x]

partial class StressTestOuter<T1>
{
    internal partial struct Something
    {
        internal partial class Component<T2> : InterfacePackAlias, IComponent<T2>, IDestroyable
        {
            public int Update2Count;
            public int DestroyCount;

            public void Destroy()
            {
                DestroyCount++;
            }

            [FilterAttribute2Alias]
            public void Update(ref T2 arg)
            {
                Update2Count++;
            }
        }
    }
}

internal partial class InterfacePack : IInitable, IComponent
{
    public int UpdateCount;
    public int InitCount;

    public void Init(Entity self)
    {
        InitCount++;
    }
    
    public void Update()
    {
        UpdateCount++;
    }
}

struct ValueTupleUniform : IUniformComponent<(string, int), (int, string)>
{
    public void Update((string, int) uniform, ref (int, string) arg)
    {
        throw new NotImplementedException();
    }
}