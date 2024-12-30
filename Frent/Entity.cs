using Frent.Core;
using Frent.Components;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Frent.Updating;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay("{DebuggerDisplayString,nq}")]
public readonly struct Entity(byte worldID, byte worldVersion, ushort version, int entityID) : IEquatable<Entity>
{
    #region Fields
    internal readonly byte WorldVersion = worldVersion;
    internal readonly byte WorldID = worldID;
    internal readonly ushort EntityVersion = version;
    //int not uint here since i might use that extra bit for enable/disable
    internal readonly int EntityID = entityID;
    #endregion

    #region Interactions
    public bool Has<T>() where T : IComponent => IsAlive(out _, out EntityLocation loc) ? 
        GlobalWorldTables.ComponentLocationTable[loc.Archetype.ArchetypeID][Component<T>.ID] == byte.MaxValue : 
        FrentExceptions.Throw_InvalidOperationException<bool>(EntityIsDeadMessage);

    public ref T Get<T>() where T : IComponent
    {
        //Total: 7x dereference

        //2x
        if (!IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        //5x
        byte compIndex = GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][Component<T>.ID];

        if (compIndex == byte.MaxValue)
            FrentExceptions.Throw_ComponentNotFoundException<T>();

        return ref ((IComponentRunner<T>)entityLocation.Archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
    }

    internal ref T? TryGetCore<T>(out bool exists)
         where T : IComponent
    {
        if(!IsAlive(out _, out EntityLocation entityLocation))
        {
            exists = false;
            return ref DefaultReference<T>.Value;
        }

        byte compIndex = GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][Component<T>.ID];

        if(compIndex == byte.MaxValue)
        {
            exists = false;
            return ref DefaultReference<T>.Value;
        }

        exists = true;
        return ref ((IComponentRunner<T>)entityLocation.Archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex]!;
    }

    public Option<T> TryGet<T>()
        where T : IComponent
    {
        ref T? value = ref TryGetCore<T>(out bool exists);
        //this can only be null if the user set something to be null
        return new Option<T>(exists, ref value!);
    }

    public bool TryGet<T>(out Ref<T> value)
        where T : IComponent
    {
        value = new Ref<T>(ref TryGetCore<T>(out bool exists)!);
        return exists;
    }

    public void Delete()
    {
        if(IsAlive(out World? world, out EntityLocation entityLocation))
        {
            world.DeleteEntityInternal(this, ref entityLocation);
        }
        else
        {
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        }
    }

    public bool IsNull => WorldID == 0 && WorldVersion == 0 && EntityID == 0 && EntityVersion == 0;
    #endregion

    #region Private Helpers
    private bool IsAlive([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        //2x dereference
        var span = GlobalWorldTables.Worlds.AsSpan();
        int worldId = WorldID;
        if (span.Length > worldId)
        {
            world = span[worldId];
            if(world.Version == WorldVersion)
            {
                var (loc, ver) = world.EntityTable[(uint)EntityID];
                if(ver == EntityVersion)
                {
                    entityLocation = loc;
                    return true;
                }

            }
        }

        entityLocation = default;
        world = null;
        return false;
    }

    internal string DebuggerDisplayString => $"World: {WorldID}, Version: {EntityVersion}, ID: {EntityID}";
    internal const string EntityIsDeadMessage = "Entity is Dead";
    #endregion

    #region IEquatable
    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    public bool Equals(Entity other) => other.WorldID == WorldID && other.EntityVersion == EntityVersion && other.EntityID == EntityID;
    public override int GetHashCode() => HashCode.Combine(WorldID, EntityVersion, EntityID);
    #endregion

    private static class DefaultReference<T> { public static T? Value; }
}