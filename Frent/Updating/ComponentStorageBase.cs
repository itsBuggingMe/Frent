using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using Frent.Updating.Runners;
using System.Runtime.CompilerServices;

namespace Frent.Updating;

internal abstract class ComponentStorageBase(Array initalBuffer)
{
    protected Array _buffer = initalBuffer;
    public Array Buffer => _buffer;
    /// <summary>
    /// Calls the Update function on every component.
    /// </summary>
    internal abstract void Run(World world, Archetype b);
    /// <summary>
    /// Calls the Update function on the subsection of components.
    /// </summary>
    internal abstract void Run(World world, Archetype b, int start, int length);
    internal abstract void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    /// <summary>
    /// Deletes a component from the storage.
    /// </summary>
    internal abstract void Delete(DeleteComponentData deleteComponentData);
    /// <summary>
    /// Resizes internal buffer to 0 and calls the destroyer on every component.
    /// </summary>
    internal abstract void Release(Archetype archetype, bool isDeferredCreate);
    /// <summary>
    /// Resizes internal buffer to the specified size. Does not call the destroyer.
    /// </summary>
    internal abstract void ResizeBuffer(int size);
    /// <summary>
    /// Only copies component. Initer and Destroyer not called.
    /// </summary>
    internal abstract void PullComponentFromAndClear(ComponentStorageBase otherRunner, int me, int other, int otherRemove);
    /// <summary>
    /// Copies component from storage without disposing component handle - just copies. Initer called.
    /// </summary>
    internal abstract void PullComponentFrom(IDTable storage, int me, int other);
    /// <summary>
    /// Invokes generic event if not null.
    /// </summary>
    internal abstract void InvokeGenericActionWith(GenericEvent? action, Entity entity, int index);
    /// <summary>
    /// Invokes the generic action if not null.
    /// </summary>
    internal abstract void InvokeGenericActionWith(IGenericAction action, int index);
    /// <summary>
    /// Note: this method is pretty specialized. It creates a component handle and calls the destroyer.
    /// </summary>
    internal abstract ComponentHandle Store(int index);
    /// <summary>
    /// Sets the component at the index. Invokes lifetime if component type isn't a struct, the new component is different, and parent is not null.
    /// </summary>
    internal abstract void SetAt(Entity? parent, object component, int index);
    /// <summary>
    /// Sets the component at the index. Invokes lifetime if component type isn't a struct, the new component is different, and parent is not null.
    /// </summary>
    internal abstract void SetAt(Entity? parent, ComponentHandle component, int index);
    /// <summary>
    /// Gets the component at the index.
    /// </summary>
    internal abstract object GetAt(int index);

    /// <summary>
    /// Calls the initer at the location.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="index"></param>
    internal abstract void CallIniter(Entity parent, int index);
    /// <summary>
    /// Implementation should mirror <see cref="ComponentStorage{T}.PullComponentFromAndClear(ComponentStorageBase, int, int, int)"/>
    /// </summary>
    internal void PullComponentFromAndClearTryDevirt(ComponentStorageBase otherRunner, int me, int other, int otherRemove)
    {
        //if (Toggle.EnableDevirt && ElementSize != -1 &&
        //        Versioning.MemoryMarshalNonGenericGetArrayDataReferenceSupported)
        //{
        //    //benchmarked to be slower
        //    //TODO: speed up devirtualized impl?
        //
        //    Debug.Assert(GetType() == otherRunner.GetType());
        //
        //    ref byte meRef = ref MemoryMarshal.GetArrayDataReference(Buffer);
        //    ref byte fromRef = ref MemoryMarshal.GetArrayDataReference(otherRunner.Buffer);
        //
        //    nint nsize = ElementSize;
        //    
        //    ref byte item = ref Unsafe.Add(ref fromRef, other * nsize);
        //    ref byte down = ref Unsafe.Add(ref fromRef, otherRemove * nsize);
        //    ref byte dest = ref Unsafe.Add(ref meRef, me * nsize);
        //
        //    // x == item, - == empty
        //    // to buffer   |   from buffer
        //    // x           |   x
        //    // x           |   x <- item
        //    // x           |   x
        //    // - <- dest   |   x <- down
        //    // -           |   -
        //
        //    //item -> dest
        //    //Unsafe.CopyBlockUnaligned(ref dest, ref item, size);
        //    //down -> item
        //    //Unsafe.CopyBlockUnaligned(ref item, ref down, size);
        //
        //    switch (ElementSize)
        //    {
        //        case 2:
        //            CopyBlock<Block2>(ref dest, ref item);
        //            CopyBlock<Block2>(ref item, ref down);
        //            return;
        //        case 4:
        //            CopyBlock<Block4>(ref dest, ref item);
        //            CopyBlock<Block4>(ref item, ref down);
        //            return;
        //        case 8:
        //            CopyBlock<Block8>(ref dest, ref item);
        //            CopyBlock<Block8>(ref item, ref down);
        //            return;
        //        case 16:
        //            CopyBlock<Block16>(ref dest, ref item);
        //            CopyBlock<Block16>(ref item, ref down);
        //            return;
        //    }
        //    //no need to clear as no gc references
        //
        //    FrentExceptions.Throw_InvalidOperationException("This should be unreachable!");
        //}

        PullComponentFromAndClear(otherRunner, me, other, otherRemove);
    }
}

internal record struct DeleteComponentData(int ToIndex, int FromIndex);