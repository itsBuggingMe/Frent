using BenchmarkDotNet.Attributes;
using Frent.Core;
using Frent;
using Frent.Systems;

namespace Frent.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class MicroBenchmark
{
    private class Categories
    {
        public const string Create = "Create";
        public const string Has = "Has";
        public const string Get = "Get";
        public const string Add = "Add";
    }

    private World _world;
    private Entity _entity;
    private Query _query;

    private Entity[] _entities;

    //[Params(10, 100, 1000, 10_000, 100_000)]
    public int Count { get; set; } = 100;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entity = _world.Create<int, double, float>(default, default, default);
        _entities = new Entity[Count];
        for(int i = 0; i < _entities.Length; i++)
        {
            _entities[i] = _world.Create(0, 0.0, 1f);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Get)]
    public void GetUnsafe()
    {
        World world = _world;
        foreach (var entity in _entities)
        {
            _ = world.GetUnsafe<float>(entity);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Get)]
    public void GetSafeAnd()
    {
        World world = _world;
        foreach (var entity in _entities)
        {
            _ = world.GetSafeAnd<float>(entity);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Get)]
    public void GetSafeIf()
    {
        World world = _world;
        foreach (var entity in _entities)
        {
            _ = world.GetSafeIfRem<float>(entity);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Get)]
    public void GetSafeIfRem()
    {
        World world = _world;
        foreach (var entity in _entities)
        {
            _ = world.GetSafeIfRem<float>(entity);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Get)]
    public void GetSafeOr()
    {
        World world = _world;
        foreach (var entity in _entities)
        {
            _ = world.GetSafeOr<float>(entity);
        }
    }

/*
    
    #region Create
    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create1()
    {
        using World w = new();
        w.EnsureCapacity<int>(100);
        for (int i = 0; i < 100; i++)
            w.Create<int>(default);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create2()
    {
        using World w = new();
        w.EnsureCapacity<int>(100);
        for (int i = 0; i < 100; i++)
            w.Create<int, int>(default, default);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create3()
    {
        using World w = new();
        w.EnsureCapacity<int>(100);
        for (int i = 0; i < 100; i++)
            w.Create<int, int, int>(default, default, default);
    }
    #endregion*/
    /*
    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Has()
    {
        //_entity.Has<int>();
    }
    */
}