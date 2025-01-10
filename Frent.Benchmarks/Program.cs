using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;

namespace Frent.Benchmarks;

public class Program
{

    static void Main(string[] args)
    {
        for(int j = 0; j < 10; j++)
        {
            using World w = new World();
            w.EnsureCapacity<int>(Iterations);
            for (int i = 0; i < Iterations; i++)
            {
                w.Create<int>(default);
            }

            Thread.Sleep(100);
        }
        for (int j = 0; j < 100; j++)
        {
            using World w = new World();
            w.EnsureCapacity<int>(Iterations);
            for (int i = 0; i < Iterations; i++)
            {
                w.Create<int>(default);
            }
        }

        return;
        BenchmarkRunner.Run<Program>();
        //new HollisticBenchmark().Setup();
    }

    private const int Iterations = 100_000;

    [Benchmark]
    public void CreateStruct256()
    {
        using World w = new World();
        for (int i = 0; i < Iterations; i++)
        {
            w.Create<Struct256>(default);
        }
    }

    [Benchmark]
    public void CreateStruct128()
    {
        using World w = new World();
        for (int i = 0; i < Iterations; i++)
        {
            w.Create<Struct128>(default);
        }
    }

    [Benchmark]
    public void CreateStruct64()
    {
        using World w = new World();
        for (int i = 0; i < Iterations; i++)
        {
            w.Create<Struct64>(default);
        }
    }

    [Benchmark]
    public void CreateStruct32()
    {
        using World w = new World();
        for (int i = 0; i < Iterations; i++)
        {
            w.Create<Struct32>(default);
        }
    }

    [Benchmark]
    public void CreateStruct16()
    {
        using World w = new World();
        for (int i = 0; i < Iterations; i++)
        {
            w.Create<Struct32>(default);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Size = 256)]
internal struct Struct256;

[StructLayout(LayoutKind.Sequential, Size = 128)]
internal struct Struct128;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct Struct64;

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal struct Struct32;
[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct Struct16;