using Frent.Core;
using Frent.Components;
using System.Runtime.CompilerServices;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

public class Update<TComp> : IRunner
    where TComp : IComponent
{
    void IRunner.Run(Array buffer, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(buffer), start);

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update();
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner.Run(Array buffer, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(buffer);

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update();
            comp = ref Unsafe.Add(ref comp, 1);
        }
    }
}

[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(GetComponentRefWithStartFrom, GetComponentRefWithStartPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
public class Update<TComp, TArg> : IRunner
    where TComp : IComponent<TArg>
{
    void IRunner.Run(Array buffer, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(buffer), start);
        ref TArg arg = ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(ref arg);
            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    void IRunner.Run(Array buffer, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(buffer);
        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(ref arg);
            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }
}