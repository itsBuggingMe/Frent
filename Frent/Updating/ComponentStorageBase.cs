using Frent.Buffers;
using System.Runtime.InteropServices;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using System.Diagnostics;
using Frent.Updating.Runners;
using System.Runtime.CompilerServices;

namespace Frent.Updating;

internal abstract class ComponentStorageBase(Array initalBuffer, int valueSize)
{
    protected Array _buffer = initalBuffer;
    public Array Buffer => _buffer;
    public readonly int ElementSize = valueSize;
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
        if (false && ElementSize != -1 &&
                Versioning.MemoryMarshalNonGenericGetArrayDataReferenceSupported)
        {
            //benchmarked to be slower
            //TODO: speed up devirtualized impl?

            Debug.Assert(GetType() == otherRunner.GetType());

            ref byte meRef = ref MemoryMarshal.GetArrayDataReference(Buffer);
            ref byte fromRef = ref MemoryMarshal.GetArrayDataReference(otherRunner.Buffer);

            uint size = (uint)ElementSize;
            nint nsize = ElementSize;
            
            ref byte item = ref Unsafe.Add(ref fromRef, other * nsize);
            ref byte down = ref Unsafe.Add(ref fromRef, otherRemove * nsize);
            ref byte dest = ref Unsafe.Add(ref meRef, me * nsize);

            // x == item, - == empty
            // to buffer   |   from buffer
            // x           |   x
            // x           |   x <- item
            // x           |   x
            // - <- dest   |   x <- down
            // -           |   -

            //item -> dest
            Unsafe.CopyBlockUnaligned(ref dest, ref item, size);
            //down -> item
            Unsafe.CopyBlockUnaligned(ref item, ref down, size);
            //no need to clear as no gc references

            return;
        }

        PullComponentFromAndClear(otherRunner, me, other, otherRemove);
    }
}

internal record struct DeleteComponentData(int ToIndex, int FromIndex);