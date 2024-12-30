using Frent.Collections;
using Frent.Core;

namespace Frent;

public partial class World : IDisposable
{
    #region Static Version Management
    private static FastStack<(byte ID, byte Version)> _recycledWorldIDs = FastStack<(byte ID, byte Version)>.Create(16);
    private static byte _nextWorldID;
    #endregion


    internal Table<(EntityLocation Location, ushort Version)> EntityTable = new Table<(EntityLocation, ushort Version)>(32);
    private Table<Archetype> WorldArchetypeTable = new Table<Archetype>(4);
    private FastStack<(int ID, ushort Version)> _recycledEntityIds = FastStack<(int, ushort)>.Create(8);
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;

    public IUniformProvider UniformProvider { get; set; }

    public World(IUniformProvider? uniformProvider = null)
    {
        UniformProvider = uniformProvider ?? NullUniformProvider.Instance;
        (ID, Version) = _recycledWorldIDs.TryPop(out var id) ? id : (_nextWorldID++, byte.MaxValue);
        IDAsUInt = ID;
        GlobalWorldTables.Worlds[ID] = this;
    }

    internal Entity CreateEntityFromLocation(in EntityLocation entityLocation)
    {
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : (_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = (entityLocation, version);
        return new Entity(ID, Version, version, id);
    }

    internal void DeleteEntityInternal(Entity entity, ref readonly EntityLocation entityLocation)
    {
        //entity is guaranteed to be alive here
        Entity replacedEntity = entityLocation.Archetype.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        EntityTable[(uint)replacedEntity.EntityID] = (entityLocation, replacedEntity.EntityVersion);
    }

    public void Update()
    {
        foreach(var element in WorldArchetypeTable.AsSpan())
        {
            if(element is not null)
            {
                element.Update();
            }
        }
    }

    internal void AddArchetype(Archetype archetype)
    {
        WorldArchetypeTable[(uint)archetype.ArchetypeID] = archetype;
    }

    public void Dispose()
    {
        _recycledWorldIDs.Push((ID, (byte)(Version - 1)));
    }

    internal class NullUniformProvider : IUniformProvider
    {
        internal static NullUniformProvider Instance { get; } = new NullUniformProvider();
        public T GetUniform<T>() => FrentExceptions.Throw_InvalidOperationException<T>("Initalize the world with an IUniformProvider in order to use uniforms");
    }
}
