using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index]; ref1 = comp;",
    "|        ref T$ ref$ = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index))[eloc.Index]; ref$ = comp$;\n|")]
[Variadic("        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);",
    "|        Component<T$>.Initer?.Invoke(concreteEntity, ref ref$);\n|")]
[Variadic("            Item1 = archetype.GetComponentSpan<T>()[initalEntityCount..],", "|            Item$ = archetype.GetComponentSpan<T$>()[initalEntityCount..],\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("in T comp", "|in T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    /// <summary>
    /// Creates an <see cref="Entity"/> with the given component(s)
    /// </summary>
    /// <returns>An <see cref="Entity"/> that can be used to acsess the component data</returns>
    [SkipLocalsInit]
    public Entity Create<T>(in T comp)
    {
        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(EntityFlags.None, out var eloc);
        var components = archetype.Components;

        //1x lookup per component
        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index]; ref1 = comp;

        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        var (id, version) = entity = RecycledEntityIds.CanPop() ? RecycledEntityIds.PopUnsafe() : new(NextEntityID++, 0);
        EntityTable[id] = new(eloc, version);
        Entity concreteEntity = new Entity(ID, version, id);
        EntityCreatedEvent.Invoke(concreteEntity);

        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);

        return concreteEntity;
    }

    public ChunkTuple<T> CreateMany<T>(int count)
    {
        if (count < 0)
            FrentExceptions.Throw_ArgumentOutOfRangeException("Must create at least 1 entity!");

        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetype(this);
        int initalEntityCount = archetype.EntityCount;

        EntityTable.EnsureCapacity(EntityCount + count);

        Span<EntityIDOnly> entities = archetype.CreateEntityLocations(count, this);

        if (EntityCreatedEvent.HasListeners)
        {
            foreach (var entity in entities)
                EntityCreatedEvent.Invoke(entity.ToEntity(this));
        }

        var chunks = new ChunkTuple<T>()
        {
            Entities = new EntityEnumerator.EntityEnumerable(this, entities),
            Item1 = archetype.GetComponentSpan<T>()[initalEntityCount..],
        };

        if(Archetype<T>.NeedsInit)
        {
            
        }

        return chunks;
    }
}