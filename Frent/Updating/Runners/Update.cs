using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class Update<TComp> : IRunner
    where TComp : IComponent
{
    public void Run(Array array, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(array);

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    public void Run(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        for (int i = length - 1; i >= 0; i--)
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
internal class Update<TComp, TArg> : IRunner
    where TComp : IComponent<TArg>
{
    public void Run(Array array, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(array);

        ref TArg arg = ref b.GetComponentDataReference<TArg>();

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update(ref arg);

            comp = ref Unsafe.Add(ref comp, 1);

            arg = ref Unsafe.Add(ref arg, 1);
        }
    }

    public void Run(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        ref TArg arg = ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update(ref arg);

            comp = ref Unsafe.Add(ref comp, 1);
            arg = ref Unsafe.Add(ref arg, 1);
        }
    }
}