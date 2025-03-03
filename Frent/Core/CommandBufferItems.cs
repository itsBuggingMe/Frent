using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;
[StructLayout(LayoutKind.Sequential, Pack = 2)]
//TODO: rename this?
internal struct EntityIDOnly(int id, ushort version)
{
    internal int ID = id;
    internal ushort Version = version;
    internal Entity ToEntity(World world) => new Entity(world.ID, Version, ID);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Init(EntityIDOnly entity)
    {
        Version = entity.Version;
        ID = entity.ID;
    }
}
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
internal record struct AddComponent(EntityIDOnly Entity, ComponentHandle ComponentHandle);
internal record struct CreateCommand(EntityIDOnly Entity, int BufferIndex, int BufferLength);