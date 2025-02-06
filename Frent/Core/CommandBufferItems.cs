using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;
[StructLayout(LayoutKind.Sequential, Pack = 1)]
//TODO: rename this?
internal struct EntityIDOnly(int id, ushort version)
{
    internal int ID = id;
    internal ushort Version = version;
    internal Entity ToEntity(World world) => new Entity(world.ID, world.Version, Version, ID);
    internal void Deconstruct(out int id, out ushort version)
    {
        id = ID;
        version = Version;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetEntity(ref Entity entity)
    {
        entity.EntityVersion = Version;
        entity.EntityID = ID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Init(Entity entity)
    {
        Version = entity.EntityVersion;
        ID = entity.EntityID;
    }
}
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
internal record struct AddComponent(EntityIDOnly Entity, ComponentID ComponentID, int Index);
internal record struct CreateCommand(EntityIDOnly Entity, int BufferIndex, int BufferLength);