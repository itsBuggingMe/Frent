using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
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
            eloc.Flags |= EntityFlags.HasHadSparseComponents;
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
    /// Creates <paramref name="entities"/>.Length entities with component <typeparamref name="T"/>.
    /// </summary>
    /// <param name="entities">The destination for the created entity handles.</param>
    /// <param name="comp">The component value every created entity starts with.</param>
    /// <remarks>Component initers and <see cref="EntityCreated"/> still run for every entity.</remarks>
    /// <variadic />
    public void CreateEntities<T>(Span<Entity> entities, in T comp)
    {
        int count = entities.Length;
        if (count == 0)
            return;

        if (!AllowStructualChanges)
        {
            for (int i = 0; i < count; i++)
                entities[i] = Create(comp);
            return;
        }

        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetypes(this).Archetype;
        int start = archetype.EntityCount;
        archetype.EnsureCapacity(start + count);
        EntityTable.EnsureCapacity(NextEntityID + count);

        ref ComponentSparseSetBase sparseTable = ref MemoryMarshal.GetArrayDataReference(WorldSparseSetTable);

        bool anySparse = !(!Component<T>.IsSparseComponent && true);

        ushort worldID = WorldID;
        for (int i = 0; i < count; i++)
        {
            ref EntityLocation eloc = ref FindNewEntityLocation(out int id);
            ref EntityIDOnly record = ref archetype.CreateEntityLocation(EntityFlags.None, out eloc);
            Entity entity = new Entity(worldID, eloc.Version, id);
            record.Version = eloc.Version;
            record.ID = id;
            entities[i] = entity;

            if (anySparse)
            {
                eloc.Flags |= EntityFlags.HasHadSparseComponents;
                ref Bitset bits = ref archetype.GetBitset(eloc.Index);
                bits = default;
                if (Component<T>.IsSparseComponent)
                    bits.Set(Component<T>.SparseSetComponentIndex);
            }

            if (Component<T>.IsSparseComponent)
                MemoryHelpers.GetSparseSet<T>(ref sparseTable)[id] = comp;
        }

        if (!Component<T>.IsSparseComponent)
        {
            archetype.GetComponentSpan<T>().Slice(start, count).Fill(comp);
        }

        if (!anySparse)
        {
            Span<Bitset> bits = archetype.SparseBitsetSpan();
            if (start < bits.Length)
                bits.Slice(start, Math.Min(count, bits.Length - start)).Clear();
        }

        bool invokeCreated = EntityCreatedEvent.HasListeners;
        if (invokeCreated || !(Component<T>.Initer is null && true))
        {
            for (int i = 0; i < count; i++)
            {
                Entity entity = entities[i];
                Component<T>.Initer?.Invoke(entity, ref Component<T>.IsSparseComponent
                    ? ref MemoryHelpers.GetSparseSet<T>(ref sparseTable)[entity.EntityID]
                    : ref archetype.GetComponentSpan<T>()[start + i]);
                if (invokeCreated)
                    EntityCreatedEvent.Invoke(entity);
            }
        }
    }

    /// <summary>
    /// Creates <paramref name="entities"/>.Length entities with default components.
    /// </summary>
    /// <inheritdoc cref="CreateEntities{T}(Span{Entity}, in T)" path="/param|remarks"/>
    /// <variadic />
    public void CreateEntities<T>(Span<Entity> entities)
        => CreateEntities(entities, default(T));
}