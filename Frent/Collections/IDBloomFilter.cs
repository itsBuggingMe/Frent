namespace Frent.Collections;

internal struct IDBloomFilter
{
    private ulong _bits;
    public void Set(ushort item) => _bits |= (1U << (item & 63));
    // true -> not in set
    // false -> maybe, maybe not
    public bool IsNotInSet(ushort item) => (_bits & (1U << (item & 63))) == 0;
    public bool IsEmpty => _bits == 0;
}