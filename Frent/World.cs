using Frent.Collections;

namespace Frent;

public partial class World
{
    internal Table<(EntityLocation Location, ushort Version)> EntityTable = new Table<(EntityLocation, ushort Version)>(32);
    private FastStack<(int ID, ushort Version)> _recycledEntityIds = FastStack<(int, ushort)>.Create(16);
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;

    public IUniformProvider UniformProvider { get; set; }

    public World(IUniformProvider? uniformProvider = null)
    {
        UniformProvider = uniformProvider ?? NullUniformProvider.Instance;
    }

    internal Entity CreateEntityFromLocation(in EntityLocation entityLocation)
    {
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : (_nextEntityID, (ushort)0);
        EntityTable[(uint)id] = (entityLocation, version);
        return new Entity(ID, Version, version, id);
    }

    internal void DeleteEntityInternal(Entity entity, ref readonly EntityLocation entityLocation)
    {
        throw new NotImplementedException();
    }

    internal class NullUniformProvider : IUniformProvider
    {
        internal static NullUniformProvider Instance { get; } = new NullUniformProvider();
        public T GetUniform<T>() => FrentExceptions.Throw_InvalidOperationException<T>("Initalize the world with an IUniformProvider in order to use uniforms");
    }
}
