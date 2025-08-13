#if NETSTANDARD2_1
#pragma warning disable CS0436 // Type conflicts with imported type
global using MemoryMarshal = System.Runtime.InteropServices.MemoryMarshal;
global using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;
#pragma warning restore CS0436 // Type conflicts with imported type

#region Attributes
using CommunityToolkit.HighPerformance;
using Frent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Frent
{
    internal class SkipLocalsInit : Attribute;
    internal class StackTraceHidden : Attribute;
}

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit : Attribute;
}
#endregion

#region Static class helpers
namespace System.Runtime.CompilerServices
{
    internal static class RuntimeHelpers
    {
        public static bool IsReferenceOrContainsReferences<T>() => Cache<T>.Value;

        private static class Cache<T>
        {
            public static readonly bool Value = IsReferenceOrContainsReferences(typeof(T));
        }

        private static bool IsReferenceOrContainsReferences(Type type)
        {
            if (!type.IsValueType)
                return true;
            if (type.IsPrimitive || type.IsPointer || type.IsPointer)
                return false;

            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(f => IsReferenceOrContainsReferences(f.FieldType));
        }
    }
}

namespace System.Runtime.InteropServices
{
    internal static class MemoryMarshal
    {
        public static ref T GetReference<T>(Span<T> span) => ref span.DangerousGetReference();
        public static ref T GetReference<T>(ReadOnlySpan<T> span) => ref span.DangerousGetReference();
        public static ref T GetArrayDataReference<T>(T[] arr) => ref arr.DangerousGetReference();
    }
}

namespace System.Numerics
{
    internal static class BitOperations
    {
        public static int LeadingZeroCount(nuint value)
        {
            if(IntPtr.Size == 8)
            {
                uint hi = (uint)(value >> 32);

                if (hi == 0)
                {
                    return 32 + LeadingZeroCount((uint)value);
                }

                return LeadingZeroCount(hi);
            }

            return LeadingZeroCount((uint)value);
        }

        private static int LeadingZeroCount(uint value)
        {
            if (value == 0)
                return 32;
            return 31 ^ Log2(value);
        }

        public static int Log2(uint value)
        {
            value |= value >> 01;
            value |= value >> 02;
            value |= value >> 04;
            value |= value >> 08;
            value |= value >> 16;

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
                ref MemoryMarshal.GetArrayDataReference(Log2DeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
        }

        public static uint RoundUpToPowerOf2(uint value)
        {
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        private static readonly byte[] Log2DeBruijn =  // 32
        [
            00, 09, 01, 10, 13, 21, 02, 29,
            11, 14, 16, 18, 22, 25, 03, 30,
            08, 12, 20, 28, 15, 17, 24, 07,
            19, 27, 23, 06, 26, 05, 04, 31
        ];
    }
}
#endregion

#endif