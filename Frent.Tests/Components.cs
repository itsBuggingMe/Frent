using Frent.Components;
using System.Runtime.CompilerServices;

namespace Frent.Tests;

struct UpdateComponent1 : IComponent<int>
{
    public void Update(ref int arg) => arg++;
}

struct UpdateComponent2 : IUpdateComponent<int, long>
{
    public void Update(ref int arg1, ref long arg2)
    {
        arg1++; 
        arg2++;
    }
}

struct UniformComponent : IUniformComponent<int, int>
{
    public void Update(in int uniform, ref int arg) => arg += uniform;
}

internal struct Component1();
internal struct Component2();
internal struct Component3();

internal struct DummyComponent();