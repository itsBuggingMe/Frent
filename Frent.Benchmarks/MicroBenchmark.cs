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

    [Params(10, 100, 1000, 10_000, 100_000)]
    public int Count { get; set; } = 100;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entities = new Entity[Count];
        foreach (ref var e in _entities.AsSpan())
        {
            e = _world.Create(0, 0.0, 0f);
        }
    }

    [Benchmark]
    public void AddRem()
    {
        foreach(var e in _entities)
        {
            e.Remove<int>();
            e.Add(0);
        }
    }

    /*
    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Has()
    {
        //_entity.Has<int>();
    }
    */
}