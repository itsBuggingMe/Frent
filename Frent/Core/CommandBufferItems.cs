using System.Runtime.InteropServices;

namespace Frent.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct EntityIDOnly(int ID, ushort Version);
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct AddOrDeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);