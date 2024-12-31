using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
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
            world.Create<Component32, Component64>(default, default);
        }

        world.Dispose();
    }

    [Benchmark]
    public void CreateAndDeleteEntities1()
    {
        World world = new World();
        var entities = _sharedEntityBuffer100k.AsSpan();
        for(int i = 0; i < entities.Length; i++)
            entities[i] = world.Create<Component32>(default);
        for (int i = 0; i < entities.Length; i++)
            entities[i].Delete();
        world.Dispose();
    }

    [Benchmark]
    public void CreateAndDeleteEntities2()
    {
        World world = new World();
        var entities = _sharedEntityBuffer100k.AsSpan();
        for (int i = 0; i < entities.Length; i++)
            entities[i] = world.Create<Component32, Component64>(default, default);
        for (int i = 0; i < entities.Length; i++)
            entities[i].Delete();
        world.Dispose();
    }
}