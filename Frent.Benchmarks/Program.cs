using System.Diagnostics;

namespace Frent.Benchmarks;

//[MemoryDiagnoser]
//[DisassemblyDiagnoser(5)]
public class Program
{
    static void Main(string[] args)
    {
    }

    private Entity[] _sharedEntityBuffer100k = null!;
    private Entity _entity;
    private World world = null!;

    //[GlobalSetup]
    public void Setup()
    {
        world = new World();

    }

    //[Benchmark]
    public void CreateMax()
    {
        World w = new();
        for (int i = 0; i < 100_000_000; i++)
        {
            w.Create(i);
        }
        w.Dispose();
    }

    //[Benchmark]
    public void Create1()
    {
        World w = new();
        for (int i = 0; i < 1_000_000; i++)
        {
            w.Create<int>(i);
        }
        w.Dispose();
    }

    //[Benchmark]
    public void Create2()
    {
        World w = new();
        for (int i = 0; i < 1_000_000; i++)
        {
            w.Create<int, float>(i, default);
        }
        w.Dispose();
    }

    //[Benchmark]
    public void Create3()
    {
        World w = new();
        for (int i = 0; i < 1_000_000; i++)
        {
            w.Create<int, float, short>(i, default, default);
        }
        w.Dispose();
    }

    //[Benchmark]
    public void Create4()
    {
        World w = new();
        for (int i = 0; i < 1_000_000; i++)
        {
            w.Create<int, float, short, long>(i, default, default, default);
        }
        w.Dispose();
    }

    /*
    [Benchmark]
    public void CreateEnsure()
    {
        World w = new();
        w.EnsureCapacity<Position, Velocity, float>(10_000);
        for (int i = 0; i < 10_000; ++i)
        {
            w.Create<Position, Velocity, float>(default, default, default);
        }
        w.Dispose();
    }

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

    internal struct QueryStruct : IQuery<Position, Velocity>
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