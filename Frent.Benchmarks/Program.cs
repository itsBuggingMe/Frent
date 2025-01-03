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
    private Entity _entity;
    private World world = null!;

    public struct Component1
    {
        public int Value;
    }

    public struct Component2
    {
        public int Value;
    }

    public struct Component3
    {
        public int Value;
    }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        int entityCount = 100_000;
        int entityPadding = 10;
        for (int i = 0; i < entityCount; ++i)
        {
            for (int j = 0; j < entityPadding; ++j)
            {
                world.Create<Component2>(default);
            }

            world.Create<Component1>(default);
        }

        _entity = world.Create<Component32>(default);
    }

    [Benchmark]
    public void RunEntities()
    {
        world.Query((ref Component32 comp) => comp.Value++);
        Console.WriteLine(_entity.Get<Component32>().Value);
    }
}