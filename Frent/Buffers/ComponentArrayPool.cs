using System.Buffers;
using System.Numerics;

namespace Frent.Buffers;
class ComponentArrayPool<T> : ArrayPool<T>
{
    public override T[] Rent(int minimumLength)
    {
        throw new NotImplementedException();
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (!BitOperations.IsPow2(array.Length))
            return;

        if (clearArray)
            array.AsSpan().Clear();

        throw new NotImplementedException();
    }
}
