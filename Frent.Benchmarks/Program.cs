using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using ArchWorld = Arch.Core.World;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(5)]
public class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Program>();
    }


    private Entity[] _sharedEntityBuffer100k = null!;
    private Entity _entity;
    private World world = null!;
    private ArchWorld _arch;
    private QueryDescription _queryDescription;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        _arch = ArchWorld.Create();
        _queryDescription = new QueryDescription().WithAll<Velocity, Position>();
        for (int i = 0; i < 1_000_000; i++)
        {
            _arch.Create<Position, Velocity>();
            world.Create<Position, Velocity>(default, default);
        }
    }

    
    [Benchmark]
    public void CreateArch()
    {
        ArchWorld aw = ArchWorld.Create();
        for (int i = 0; i < 1_000_000; ++i)
        {
            aw.Create<Position, Velocity>();
        }
        ArchWorld.Destroy(aw);
    }

    [Benchmark]
    public void Create()
    {
        World w = new();
        for (int i = 0; i < 1_000_000; ++i)
        {
            w.Create<Position, Velocity, double>(default, default, default);
        }
        w.Dispose();
    }


    /*
    [Benchmark]
    public void RunEntities()
    {
        world.Query((ref Position comp, ref Velocity vel) => comp.X += vel.DX);
    }

    [Benchmark]
    public void RunEntitiesInline()
    {
        world.InlineQuery<QueryStruct, Position, Velocity>(default);
    }

    [Benchmark]
    public void RunEntitiesArch()
    {
        _arch.Query(_queryDescription, (ref Position comp, ref Velocity vel) => comp.X += vel.DX);
    }

    [Benchmark]
    public void RunEntitiesInlineArch()
    {
        _arch.InlineQuery<QueryStruct, Position, Velocity>(_queryDescription);
    }*/

    internal struct QueryStruct : IQuery<Position, Velocity>, IForEach<Position, Velocity>
    {
        public void Run(ref Position arg1, ref Velocity arg2) => arg1.X += arg2.DX;
        public void Update(ref Position t0, ref Velocity t1) => t0.X += t1.DX;
    }

    internal struct Position(float x)
    {
        public float X = x;
    }

    internal struct Velocity(float dx)
    {
        public float DX = dx;
    }
}