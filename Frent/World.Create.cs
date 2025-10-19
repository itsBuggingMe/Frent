using Frent.Collections;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;


[Variadic(nameof(World))]
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
        WorldArchetypeTableItem archetypes = Archetype<T>.CreateNewOrGetExistingArchetypes(this);

        ref var archetypeEntityRecord = ref Unsafe.NullRef<EntityIDOnly>();
        ref EntityLocation eloc = ref FindNewEntityLocation(out int id);

        ComponentStorageRecord[] components;
        Archetype inserted;

        if (AllowStructualChanges)
        {
            inserted = archetypes.Archetype;
            components = archetypes.Archetype.Components;
            archetypeEntityRecord = ref archetypes.Archetype.CreateEntityLocation(EntityFlags.None, out eloc);
        }
        else
        {
            // we don't need to manually set flags, they are already zeroed
            archetypeEntityRecord = ref archetypes.Archetype.CreateDeferredEntityLocation(this, archetypes.DeferredCreationArchetype,
                ref eloc,
                out components,
                out inserted);
            DeferredCreationEntities.Push(id);
        }

        archetypeEntityRecord.Version = eloc.Version;
        archetypeEntityRecord.ID = id;

        ref ComponentSparseSetBase start = ref MemoryMarshal.GetArrayDataReference(WorldSparseSetTable);

        //1x array lookup per component
        ref T ref1 = ref Component<T>.IsSparseComponent ?
            ref MemoryHelpers.GetSparseSet<T>(ref start)[id]
            : ref components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index).UnsafeIndex<T>(eloc.Index);
        ref1 = comp;

        bool hasSparseComponent = !(!Component<T>.IsSparseComponent && true);

        if (hasSparseComponent)
        {
            eloc.Flags |= EntityFlags.HasSparseComponents;
            ref Bitset bitset = ref inserted.GetBitset(eloc.Index);

            bitset = default;

            if (Component<T>.IsSparseComponent) bitset.Set(Component<T>.SparseSetComponentIndex);
        }
        else
        {
            inserted.ClearBitset(eloc.Index);
        }

        // Version is incremented on delete, so we don't need to do anything here
        Entity concreteEntity = new Entity(WorldID, eloc.Version, id);

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
    [Obsolete]
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
            Entities = new EntityEnumerator(this, entities),
            Span = archetypes.Archetype.GetComponentSpan<T>()[initalEntityCount..],
        };

        return chunks;
    }
}