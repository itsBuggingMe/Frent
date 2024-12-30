using Frent.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

internal static partial class UpdateHelper<T>
{
    public static IComponentRunner<T> CreateInstance() => UpdaterInstance.CloneStronglyTyped();
    public static readonly IComponentRunner<T> UpdaterInstance;
    static UpdateHelper()
    {
        if(GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out IComponentRunner? type))
        {
            if(type is IComponentRunner<T> casted)
            {
                UpdaterInstance = casted;
                return;
            }

            throw new InvalidOperationException($"{typeof(T).FullName} not initalized properly. Please create an issue on Github.");
        }

        throw new InvalidOperationException($"{typeof(T).FullName} is not initalized. (Is the source generator working?)");
    }
}

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

    internal static IComponentRunner GetComponentRunnerFromType(Type t)
    {
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out IComponentRunner? type))
        {
            return type.Clone();
        }

        throw new InvalidOperationException($"{t.FullName} is not initalized. (Is the source generator working?)");
    }
}