using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Frent.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(3)]
public class Program
{
    static void Main(string[] args) => BenchmarkRunner.Run<Program>();

    private Entity[] _sharedEntityBuffer100k = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sharedEntityBuffer100k = new Entity[100_000];
    }

    [Benchmark]
    public void CreateEntities1()
    {
        World world = new World();
        for(int i = 0; i < 1000; i++)
            world.Create<Component32>(default);

        for(int i = 0; i < 1000; i++)
            world.Update();

        world.Dispose();
    }
}