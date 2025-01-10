using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;

namespace Frent.Benchmarks;

public class Program
{

    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Program>();
    }       

    [Benchmark]
    public void Create()
    {
        using World w = new World();
        w.EnsureCapacity<int>(100_000);
        for (int i = 0; i < 100_000; i++)
        {
            w.Create<int>(default);
        }
    }
}