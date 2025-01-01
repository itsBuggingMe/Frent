using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ArchWorld = Arch.Core.World;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(3)]
public class Program
{
    static void Main(string[] args) => BenchmarkRunner.Run<Program>();

    private Entity[] _sharedEntityBuffer100k = null!;
    private World world = null!;
    private ArchWorld worlda = null!;
    private QueryDescription _query;

    [GlobalSetup]
    public void Setup()
    {
        worlda = ArchWorld.Create();
        world = new World();
        _sharedEntityBuffer100k = new Entity[100_000];
        for(int i = 0; i < _sharedEntityBuffer100k.Length; i++)
        {
            worlda.Create<Component32>();
            _sharedEntityBuffer100k[i] = world.Create<Component32>(default);
        }

        _query = new QueryDescription().WithAll<Component32>();

    }

    [Benchmark]
    public void RunEntities()
    {
        world.InlineQuery<Nothing>(default(Nothing));
    }

    [Benchmark]
    public void RunEntitiesArch()
    {
        worlda.InlineQuery<Nothing>(_query);
    }

    internal struct Nothing : IForEach, IQueryEntity
    {
        public void Run(Entity entity)
        {

        }

        public void Update(Arch.Core.Entity entity)
        {

        }
    }
}