using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frent.Core;

namespace Frent.Collections;

// triple paginated sparse array
// page size is 16
internal class SparseArray<T>
{
    private const int PageSize = 16;
    private T[][][] _pages = [];

    public ref T Set(int index)
    {
        //each inner page stores 16*16 = 256 elements

        int outerPageIndex = index >> 8;

        ref T[][] inner = ref MemoryHelpers.GetValueOrResize(_pages, outerPageIndex);
        inner ??= new T[PageSize][];

        int innerPageIndex = outerPageIndex & 0xFF; //index % 256
        ref T[] dataArray = ref MemoryHelpers.GetValueOrResize(inner, innerPageIndex);
        dataArray ??= new T[PageSize];

        int innerIndex = index & 0xFF;

        return ref dataArray[innerIndex];
    }
}