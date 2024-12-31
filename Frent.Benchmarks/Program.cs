using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ArchWorld = Arch.Core.World;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class Program
{
    static void Main(string[] args) => BenchmarkRunner.Run<Program>();

    [Benchmark]
    public void CreateEntities1()
    {
        World world = new World();

        for(int i = 0; i < 100_000; i++)
        {
            world.Create<Component32>(default);
        }

        world.Dispose();
    }

    [Benchmark]
    public void CreateEntities2()
    {
        World world = new World();

        for (int i = 0; i < 100_000; i++)
        {
            world.Create<Component32, Component32>(default, default);
        }

        world.Dispose();
    }
}