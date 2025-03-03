namespace Frent.Core;

internal static class UnsafeExtensions
{
#if !DEBUG
    public static ref T UnsafeArrayIndex<T>(this T[] arr, nint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), index);
    public static ref T UnsafeArrayIndex<T>(this T[] arr, int index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), index);
    public static ref T UnsafeArrayIndex<T>(this T[] arr, ushort index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), index);
    public static ref T UnsafeArrayIndex<T>(this T[] arr, uint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), index);
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, int index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetReference(arr), index);
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, ushort index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetReference(arr), index);
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, uint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetReference(arr), index);
    public static T UnsafeCast<T>(object o) where T : class =>
        Unsafe.As<T>(o);
#else
    public static ref T UnsafeArrayIndex<T>(this T[] arr, nint index) =>
        ref arr[index];
    public static ref T UnsafeArrayIndex<T>(this T[] arr, int index) =>
        ref arr[index];
    public static ref T UnsafeArrayIndex<T>(this T[] arr, ushort index) =>
        ref arr[index];
    public static ref T UnsafeArrayIndex<T>(this T[] arr, uint index) =>
        ref arr[index];
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, int index) =>
        ref arr[index];
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, ushort index) =>
        ref arr[index];
    public static ref T UnsafeSpanIndex<T>(this Span<T> arr, uint index) =>
        ref arr[(int)index];
    public static T UnsafeCast<T>(object o) where T : class =>
        (T)o;
#endif
}
