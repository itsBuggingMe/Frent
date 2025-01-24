using System.Runtime.InteropServices;

namespace Frent.Core;
[StructLayout(LayoutKind.Sequential, Pack = 1)]

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
}
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
internal record struct AddComponent(EntityIDOnly Entity, ComponentID ComponentID, int Index);
internal record struct CreateCommand(int BufferIndex, int BufferLength);