using Frent.Collections;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;


[Variadic("World.create")]
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
        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        var (id, version) = RecycledEntityIds.CanPop() ? RecycledEntityIds.PopUnsafe() : new(NextEntityID++, 0);
        ref EntityLocation eloc = ref EntityTable[id];
        eloc.Version = version;

        WorldArchetypeTableItem archetypes = Archetype<T>.CreateNewOrGetExistingArchetypes(this);

        ref var entity = ref Unsafe.NullRef<EntityIDOnly>();

        ComponentStorageRecord[] components;

        if (AllowStructualChanges)
        {
            components = archetypes.Archetype.Components;
            entity = ref archetypes.Archetype.CreateEntityLocation(EntityFlags.None, out eloc);
        }
        else
        {
            // we don't need to manually set flags, they are already zeroed
            entity = ref archetypes.Archetype.CreateDeferredEntityLocation(this, archetypes.DeferredCreationArchetype, ref eloc, out components);
            DeferredCreationEntities.Push(id);
        }

        entity = new EntityIDOnly(id, version);

        bool hasSparseComponent = !(!Component<T>.IsSparseComponent && true);

        if (hasSparseComponent)
        {
            eloc.Flags |= EntityFlags.HasSparseComponents;
        }


        ref ComponentSparseSetBase start = ref MemoryMarshal.GetArrayDataReference(WorldSparseSetTable);

        //1x array lookup per component
        ref T ref1 = ref Component<T>.IsSparseComponent ?
            ref MemoryHelpers.GetSparseSet<T>(ref start)[id] 
            : ref components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index).UnsafeIndex<T>(eloc.Index);
        ref1 = comp;

        if (hasSparseComponent)
        {
            ref Bitset bitset = ref MemoryHelpers.GetBitset(ref SparseComponentTable, id);

            if (Component<T>.IsSparseComponent) bitset.Set(Component<T>.SparseSetComponentIndex);
        }

        Entity concreteEntity = new Entity(WorldID, version, id);
        
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
            Entities = new EntityEnumerator.EntityEnumerable(this, entities),
            Span = archetypes.Archetype.GetComponentSpan<T>()[initalEntityCount..],
        };
        
        return chunks;
    }
}