using Frent.Buffers;
using System.Runtime.InteropServices;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using System.Diagnostics;
using Frent.Updating.Runners;
using System.Runtime.CompilerServices;
using static Frent.Core.MemoryHelpers;
using System.Numerics;

namespace Frent.Updating;

internal abstract class ComponentStorageBase(Array initalBuffer)
{
    protected Array _buffer = initalBuffer;
    public Array Buffer => _buffer;
    internal abstract void Run(World world, Archetype b);
    internal abstract void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    internal abstract void Delete(DeleteComponentData deleteComponentData);
    internal abstract void Trim(int chunkIndex);
    internal abstract void ResizeBuffer(int size);
    internal abstract void PullComponentFromAndClear(ComponentStorageBase otherRunner, int me, int other, int otherRemove);
    internal abstract void PullComponentFrom(IDTable storage, int me, int other);
    internal abstract void InvokeGenericActionWith(GenericEvent? action, Entity entity, int index);
    internal abstract void InvokeGenericActionWith(IGenericAction action, int index);
    internal abstract ComponentHandle Store(int index);
    internal abstract void SetAt(object component, int index);
    internal abstract object GetAt(int index);
    internal abstract ComponentID ComponentID { get; }


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

    internal static int GetComponentSize<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            return -1;
        int size = Unsafe.SizeOf<T>();

        if((size & (size - 1)) != 0)
        {//is not power of two
            return -1;
        }

        if(size > 16 || size < 2)
        {//we have block sizes 2, 4, 8, 16
            return -1;
        }

        return size;
    }
}

internal record struct DeleteComponentData(int ToIndex, int FromIndex);