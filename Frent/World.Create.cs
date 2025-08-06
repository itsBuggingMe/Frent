using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ref T ref1 = ref components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index).UnsafeIndex<T>(eloc.Index); ref1 = comp;",
    "|        ref T$ ref$ = ref components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index).UnsafeIndex<T$>(eloc.Index); ref$ = comp$;\n|")]
[Variadic("        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);",
    "|        Component<T$>.Initer?.Invoke(concreteEntity, ref ref$);\n|")]
[Variadic("            Span = archetypes.Archetype.GetComponentSpan<T>()[initalEntityCount..],", "|            Span$ = archetypes.Archetype.GetComponentSpan<T$>()[initalEntityCount..],\n|")]
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
    /// <variadic />
    [SkipLocalsInit]
    public Entity Create<T>(in T comp)
    {
        var archetypes = Archetype<T>.CreateNewOrGetExistingArchetypes(this);

        ref var archetypeEntityRecord = ref Unsafe.NullRef<EntityIDOnly>();
        ref EntityLocation eloc = ref FindNewEntityLocation(out int id);

        ComponentStorageRecord[] components;

        if (AllowStructualChanges)
        {
            components = archetypes.Archetype.Components;
            archetypeEntityRecord = ref archetypes.Archetype.CreateEntityLocation(EntityFlags.None, out eloc);
        }
        else
        {
            // we don't need to manually set flags, they are already zeroed
            archetypeEntityRecord = ref archetypes.Archetype.CreateDeferredEntityLocation(this, archetypes.DeferredCreationArchetype, ref eloc, out components);
        }

        archetypeEntityRecord.Version = eloc.Version;
        archetypeEntityRecord.ID = id;

        //1x array lookup per component
        ref T ref1 = ref components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index).UnsafeIndex<T>(eloc.Index); ref1 = comp;

        // Version is incremented on delete, so we don't need to do anything here
        Entity concreteEntity = new Entity(ID, eloc.Version, id);
        
        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);
        EntityCreatedEvent.Invoke(concreteEntity);

        return concreteEntity;
    }

    /// <summary>
    /// Creates a large amount of entities quickly
    /// </summary>
    /// <param name="count">The number of entities to create</param>
    /// <returns>The entities created and their component spans</returns>
    /// <variadic />
    public ChunkTuple<T> CreateMany<T>(int count)
    {
        if (count < 0)
            FrentExceptions.Throw_ArgumentOutOfRangeException("Must create at least 1 entity!");
        if (!AllowStructualChanges)
            FrentExceptions.Throw_InvalidOperationException("Cannot bulk create during world updates!");

        var archetypes = Archetype<T>.CreateNewOrGetExistingArchetypes(this);
        int initalEntityCount = archetypes.Archetype.EntityCount;
        
        EntityTable.EnsureCapacity(EntityCount + count);
        
        Span<EntityIDOnly> entities = archetypes.Archetype.CreateEntityLocations(count, this);
        
        if (EntityCreatedEvent.HasListeners)
        {
            foreach (var entity in entities)
                EntityCreatedEvent.Invoke(entity.ToEntity(this));
        }
        
        var chunks = new ChunkTuple<T>()
        {
            Entities = new EntityEnumerator.EntityEnumerable(this, entities),
            Span = archetypes.Archetype.GetComponentSpan<T>()[initalEntityCount..],
        };
        
        return chunks;
    }
}