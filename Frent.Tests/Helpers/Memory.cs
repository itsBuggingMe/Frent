using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Frent.Tests.Helpers;
internal static class Memory
{
    private static long _bytesAllocated;
    public static void Record()
    {
        GC.Collect();
        _bytesAllocated = GC.GetAllocatedBytesForCurrentThread();
    }

    public static void AllocatedAtLeast(long bytesAllocated)
    {
        Assert.That(MeasureAllocated(), Is.GreaterThanOrEqualTo(bytesAllocated));
    }

    public static void AllocatedLessThan(long bytesAllocated)
    {
        Assert.That(MeasureAllocated(), Is.LessThan(bytesAllocated));
    }

    public static void Allocated()
    {
        Assert.That(MeasureAllocated(), Is.GreaterThan(0));
    }

    public static void NotAllocated()
    {
        Assert.That(MeasureAllocated(), Is.EqualTo(0));
    }

    private static long MeasureAllocated()
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread() - _bytesAllocated;
        TestContext.WriteLine($"{TestContext.CurrentContext.Test.Name} Allocated: {allocated}");
        return allocated;
    }
}
