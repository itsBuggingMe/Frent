using Frent.Core;
using Frent.Components;
using System.Runtime.CompilerServices;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;
public class UniformUpdate<TComp, TUniform> : IRunner
    where TComp : IUniformComponent<TUniform>
{
    void IRunner.Run(Array buffer, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(buffer), start);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(uniform);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.Run(Array buffer, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(buffer);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(uniform);
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(GetComponentRefWithStartFrom, GetComponentRefWithStartPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
public class UniformUpdate<TComp, TUniform, TArg> : IRunner
    where TComp : IUniformComponent<TUniform, TArg>
{
    void IRunner.Run(Array buffer, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(buffer), start);
        ref TArg arg = ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(uniform, ref arg);
            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    void IRunner.Run(Array buffer, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(buffer);
        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(uniform, ref arg);
            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }
}