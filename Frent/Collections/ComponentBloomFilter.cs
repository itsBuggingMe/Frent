using System;

namespace Frent.Collections;

internal struct BloomFilter
{
    private ulong _bits;
    public void Set(ushort item) => _bits |= (1 << (item & 63));
    // true -> not in set
    // false -> maybe, maybe not
    public bool IsNotInSet(ushort item) => (_bits & (1 << (item & 63))) == 0;
}
