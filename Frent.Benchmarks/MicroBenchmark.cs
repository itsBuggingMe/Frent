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
        _entity = _world.Create<int, double>(default, default);
        _entities = new Entity[100_000];
        for (int i = 0; i < _entities.Length; i++)
        {
            _entities[i] = _world.Create(0, 0f, 0.0, "", (0f, 0f));
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Add)]
    public void AddRem()
    {
        foreach (var entity in _entities)
        {
            entity.Remove<int>();
            entity.Add(0);
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