using System.Runtime.InteropServices;

namespace Frent.Core;

internal record struct EntityIDOnly(int ID, ushort Version)
{
    internal Entity ToEntity(World world) => new Entity(world.ID, world.Version, Version, ID);
}
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
internal record struct AddComponent(EntityIDOnly Entity, ComponentID ComponentID, int Index);
internal record struct CreateCommand(int BufferIndex, int BufferLength);