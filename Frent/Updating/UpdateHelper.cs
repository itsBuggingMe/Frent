using Frent.Buffers;
using Frent.Updating.Runners;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

internal static class UpdateHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<Chunk<TTo>> SpanCast<TTo, TFrom>(Span<Chunk<TFrom>> from)
    {
        if (typeof(TTo) != typeof(TFrom))
            throw new ArgumentException("Hey! Thats not very safe of you", nameof(from));

        Span<Chunk<TTo>> self = MemoryMarshal.CreateSpan(
                ref Unsafe.As<Chunk<TFrom>, Chunk<TTo>>(ref MemoryMarshal.GetReference(from)),
                from.Length);

        return self;
    }
}