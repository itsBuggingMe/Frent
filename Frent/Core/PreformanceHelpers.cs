﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

public static class PreformanceHelpers
{
    public static int MaxArchetypeChunkSize = 16384*4;
    public static uint RoundDownToPowerOfTwo(uint value) => BitOperations.RoundUpToPowerOf2((value >> 1) + 1);

    public static int GetSizeOfType<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            //a class means at least ~68 bytes are used (assuming stuff is pretty fragmented)
            //can't really get the size of the type
            return 68;
        }

        return Marshal.SizeOf<T>();
    }
}
