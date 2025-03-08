using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[index]; ref1 = comp;",
    "|        ref T$ ref$ = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index))[index]; ref$ = comp$;\n|")]
[Variadic("        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);",
    "|        Component<T$>.Initer?.Invoke(concreteEntity, ref ref$);\n|")]
[Variadic("            Span = archetype.GetComponentSpan<T>()[initalEntityCount..],", "|            Span$ = archetype.GetComponentSpan<T$>()[initalEntityCount..],\n|")]
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

        ref var entity = ref Unsafe.NullRef<EntityIDOnly>();
        EntityLocation eloc = default;

        ComponentStorageBase[] components;
        Unsafe.SkipInit(out int index);
        MemoryHelpers.Poison(ref index);

        if (AllowStructualChanges)
        {
            components = archetype.Components;
            entity = ref archetype.CreateEntityLocation(EntityFlags.None, out eloc);
            index = eloc.Index;
        }
        else
        {
            entity = ref archetype.CreateDeferredEntityLocation(this, ref eloc, out index, out components);
            eloc.Archetype = DeferredCreateArchetype;
        }

        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        var (id, version) = entity = RecycledEntityIds.CanPop() ? RecycledEntityIds.PopUnsafe() : new(NextEntityID++, 0);
        eloc.Version = version;
        EntityTable[id] = eloc;

        //1x array lookup per component
        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[index]; ref1 = comp;

        Entity concreteEntity = new Entity(ID, version, id);
        
        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);
        EntityCreatedEvent.Invoke(concreteEntity);

        return concreteEntity;
    }

    /// <summary>
    /// Creates a large amount of entities quickly
    /// </summary>
    /// <param name="count">The number of entities to create</param>
    /// <returns>The entities created and their component spans</returns>
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
            Span = archetype.GetComponentSpan<T>()[initalEntityCount..],
        };

        return chunks;
    }
}