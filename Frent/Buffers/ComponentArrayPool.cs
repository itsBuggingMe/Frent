using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Buffers;

internal class ComponentArrayPool<T> : ArrayPool<T>
{
    //16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536 
    //13 array sizes
    private static T[][] Buckets = new T[13][];

    public override T[] Rent(int minimumLength)
    {
        //valid chunk sizes only
        Debug.Assert(BitOperations.IsPow2(minimumLength));
        Debug.Assert(minimumLength <= 65536);

        if (minimumLength < 16)
            return new T[minimumLength];

        int bucketIndex = BitOperations.Log2((uint)minimumLength) - 4;

        return Buckets[bucketIndex] ?? new T[minimumLength];//GC.AllocateUninitializedArray<T>(minimumLength)
        //benchmarks say uninit is the same speed
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        Debug.Assert(clearArray == RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        int bucketIndex = BitOperations.Log2((uint)array.Length) - 4;
        if ((uint)bucketIndex < (uint)Buckets.Length)
            Buckets[bucketIndex] = array;
    }
}
