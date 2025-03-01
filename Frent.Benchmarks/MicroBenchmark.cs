using BenchmarkDotNet.Attributes;
using Frent.Core;
using Frent;
using Frent.Systems;
using static Frent.Benchmarks.Program;
using Frent.Updating;

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
    private int[] _raw;

    //[Params(1, 100, 10_000, 100_000)]
    public int Count { get; set; } = 100_000;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entity = _world.Create<Component1, Component2, Component3>(default, default, default);
        _entities = new Entity[Count];
        for (int i = 0; i < _entities.Length; i++)
        {
            _entities[i] = _world.Create<Component1, Component2, Component3>(default, default, default);
        }

        foreach (var entity in _entities)
        {
            entity.Remove<Component1>();
            entity.Add<Component1>(default);
        }
    }

    [Benchmark]
    public void Decon()
    {
        foreach(var entity in _entities)
        {
            entity.Deconstruct<Component1, Component2, Component3>(out var _, out var _, out var _);
        }
    }

    [Benchmark]
    public void Norm()
    {

    }

    /*
    [Benchmark]
    [BenchmarkCategory(Categories.Add)]
    public void Create()
    {
        for(int i = 0; i < 100; i++)
        {
            _world.Create(0);
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
    }*/

    /*
    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Has()
    {
        //_entity.Has<int>();
    }
    */

    internal struct Increment : IAction<Component1>
    {
        public void Run(ref Component1 arg) => arg.Value++;
    }
}