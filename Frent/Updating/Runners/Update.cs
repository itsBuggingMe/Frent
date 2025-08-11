using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public class UpdateRunner<TComp> : IRunner
    where TComp : IComponent
{
    ComponentID IRunner.ComponentID => Component<TComp>.ID;

    void IRunner. Run(Array array, Archetype b, World world)
    {
        ref TComp comp = ref IRunner.GetComponentStorageDataReference<TComp>(array);

        for (int i = b.EntityCount - 1; i >= 0; i--)
        {
            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }

    void IRunner. RunArchetypical(Array array, Archetype b, World world, int start, int length)
    {
        ref TComp comp = ref Unsafe.Add(ref IRunner.GetComponentStorageDataReference<TComp>(array), start);

        for (int i = length - 1; i >= 0; i--)
        {
            comp.Update();

            comp = ref Unsafe.Add(ref comp, 1);
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
[Variadic(GetComponentRefFrom, GetComponentRefPattern)]
[Variadic(GetComponentRefWithStartFrom, GetComponentRefWithStartPattern)]
[Variadic(IncRefFrom, IncRefPattern)]
[Variadic(TArgFrom, TArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
public class UpdateRunner<TComp, TArg> : IRunner
    where TComp : IComponent<TArg>
{
    ComponentID IRunner.ComponentID => Component<TComp>.ID;

    void IRunner. Run(Array array, Archetype b, World world)
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

    void IRunner. RunArchetypical(Array array, Archetype b, World world, int start, int length)
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