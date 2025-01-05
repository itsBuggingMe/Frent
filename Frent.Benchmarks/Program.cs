using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(5)]
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

        for (int i = 0; i < 100_000; ++i)
        {
            world.Create<Component32>(default);
        }
    }

    [Benchmark]
    public void RunEntities()
    {
        world.Query((ref Component32 comp) => comp.Value++);
    }

    [Benchmark]
    public void RunEntitiesInline()
    {
        world.InlineQuery<QueryStruct, Component32>(default);
    }

    internal struct QueryStruct : IQuery<Component32>
    {
        public void Run(ref Component32 arg)
        {
            arg.Value++;
        }
    }
}