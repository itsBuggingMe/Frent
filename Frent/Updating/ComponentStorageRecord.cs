using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;

namespace Frent.Updating;

internal struct ComponentStorageRecord
{
    // not readonly - resize
    // we reduce another level of pointer indirection by "inlining" these fields
    public Array Buffer;
    public readonly ComponentBufferManager BufferManager;

    public ComponentStorageRecord(Array array, ComponentBufferManager bufferManager)
    {
        Buffer = array;
        BufferManager = bufferManager;
    }

    internal void ResizeBuffer(int size) => BufferManager.ResizeBuffer(ref Buffer, size);
    internal readonly void Run(World world, Archetype b) => BufferManager.Run(Buffer, world, b);
    internal readonly void Run(World world, Archetype b, int start, int length) => BufferManager.Run(Buffer, world, b, start, length);
    internal readonly void Delete(DeleteComponentData deleteComponentData) => BufferManager.Delete(Buffer, deleteComponentData);
    internal readonly void Release(Archetype archetype, bool isDeferredCreate) => BufferManager.Release(Buffer, archetype, isDeferredCreate);
    internal readonly void PullComponentFromAndClear(Array otherRunner, int me, int other, int otherRemove) => BufferManager.PullComponentFromAndClear(Buffer, otherRunner, me, other, otherRemove);
    internal readonly void PullComponentFrom(IDTable storage, int me, int other) => BufferManager.PullComponentFrom(Buffer, storage, me, other);
    internal readonly void InvokeGenericActionWith(GenericEvent? action, Entity entity, int index) => BufferManager.InvokeGenericActionWith(Buffer, action, entity, index);
    internal readonly void InvokeGenericActionWith(IGenericAction action, int index) => BufferManager.InvokeGenericActionWith(Buffer, action, index);
    internal readonly ComponentHandle Store(int index) => BufferManager.Store(Buffer, index);
    internal readonly void SetAt(Entity? parent, object component, int index) => BufferManager.SetAt(Buffer, parent, component, index);
    internal readonly void SetAt(Entity? parent, ComponentHandle component, int index) => BufferManager.SetAt(Buffer, parent, component, index);
    internal readonly object GetAt(int index) => Buffer.GetValue(index)!;
    internal readonly void CallIniter(Entity parent, int index) => BufferManager.CallIniter(Buffer, parent, index);
}
