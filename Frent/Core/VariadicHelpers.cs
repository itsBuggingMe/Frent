using Frent.Core.Structures;
using Frent.Updating;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Frent.Core;

internal static class VariadicHelpers
{
    #region Archetype.static
    internal static void CreateArchetypeBuffers(ImmutableArray<ComponentID> archetypeComponentIds, ArchetypeID archetypeID, 
        out ComponentStorageRecord[] runners,
        out ComponentStorageRecord[] tempStorages,
        out byte[] map)
    {
        runners = new ComponentStorageRecord[archetypeComponentIds.Length + 1];
        tempStorages = new ComponentStorageRecord[runners.Length];
        map = GlobalWorldTables.ComponentTagLocationTable[archetypeID.RawIndex];
    }

    internal static World.WorldArchetypeTableItem CreateArchetype(ArchetypeID archetypeID, World world, ComponentStorageRecord[] runners, ComponentStorageRecord[] tmpStorages)
    {
        Archetype archetype = new Archetype(archetypeID, runners, false);
        Archetype tempCreateArchetype = new Archetype(archetypeID, tmpStorages, true);

        world.ArchetypeAdded(archetype, tempCreateArchetype);
        return new World.WorldArchetypeTableItem(archetype, tempCreateArchetype);
    }
    #endregion

    #region Runners
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TArg ArchetypeRefOrNullRef<TArg>(Archetype b, int start)
    {
        return ref Component<TArg>.IsSparseComponent ?
            ref Unsafe.NullRef<TArg>()
            : ref Unsafe.Add(ref b.GetComponentDataReference<TArg>(), start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TArg GetRefSparseOrArchetypical<TArg>(
        ref TArg sparseFirst,
        Span<int> sparseArgArray,
        in Entity entity,
        in Entity.EntityLookup entityData)
    {
        return ref Component<TArg>.IsSparseComponent
            ? ref Unsafe.Add(ref sparseFirst, sparseArgArray.UnsafeSpanIndex(entity.EntityID))
            : ref entityData.Get<TArg>();
    }
    #endregion
}
