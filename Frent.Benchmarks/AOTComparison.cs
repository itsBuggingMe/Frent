using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Diagnostics;

namespace Frent.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[DisassemblyDiagnoser]
public class AOTComparison
{
    const int ITERATIONS = 1000;

    [GlobalSetup]
    public void Setup()
    {
        Debug.Assert(IsIMarker<DoesImplement>());
        Debug.Assert(!IsIMarker<DoesntImplement>());
    }

    [Benchmark]
    public bool Is()
    {
        bool x = false;
        for(int i = 0; i < ITERATIONS; i++)
        {
            x |= IsIMarker<DoesImplement>();
        }
        return x;
    }

    [Benchmark]
    public bool IsNot()
    {
        bool x = false;
        for (int i = 0; i < ITERATIONS; i++)
        {
            x |= IsIMarker<DoesntImplement>();
        }
        return x;
    }

    [Benchmark]
    public bool ControlFalse()
    {
        bool x = false;
        for (int i = 0; i < ITERATIONS; i++)
        {
            x |= ConstantFalse();
        }
        return x;
    }

    [Benchmark]
    public bool ControlTrue()
    {
        bool x = false;
        for (int i = 0; i < ITERATIONS; i++)
        {
            x |= ConstantTrue();
        }
        return x;
    }

    private static bool IsIMarker<T>()
    {
        if(!typeof(T).IsValueType)
            throw new NotImplementedException();
        return default(T) is IMarker;
    }

    private static bool ConstantFalse()
    {
        return false;
    }

    private static bool ConstantTrue()
    {
        return true;
    }

    internal struct DoesntImplement;
    internal struct DoesImplement : IMarker;
    internal interface IMarker;
}