using System.Runtime.InteropServices;

namespace Frent.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct EntityIDOnly(int ID, ushort Version)
{
    internal Entity ToEntity(World world) => new Entity(world.ID, world.Version, Version, ID);
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct AddComponent(EntityIDOnly Entity, ComponentID ComponentID, int Index);