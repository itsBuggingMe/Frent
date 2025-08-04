﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;
[StructLayout(LayoutKind.Sequential, Pack = 2)]
//TODO: rename this?
internal struct EntityIDOnly(int id, ushort version) : IEquatable<EntityIDOnly>
{
    internal int ID = id;
    internal ushort Version = version;
    internal Entity ToEntity(World world) => new Entity(world.WorldID, Version, ID);
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

    public bool Equals(EntityIDOnly other) => other.ID == ID && other.Version == Version;
    public override int GetHashCode() => ID ^ (Version << 16);
}
internal record struct DeleteComponent(EntityIDOnly Entity, ComponentID ComponentID);
internal record struct AddComponent(EntityIDOnly Entity, ComponentHandle ComponentHandle);
internal record struct TagCommand(EntityIDOnly Entity, TagID TagID);
internal record struct CreateCommand(EntityIDOnly Entity, int BufferIndex, int BufferLength);