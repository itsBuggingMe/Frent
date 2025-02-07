﻿using BenchmarkDotNet.Attributes;
using Frent.Core;
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
    }

    private World _world;
    private Entity _entity;
    private Query _query;

    private Entity[] _entities;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entity = _world.Create<int, double>(default, default);
        _entities = new Entity[100_000];
        for(int i = 0; i < _entities.Length; i++)
        {
            _entities[i] = _world.Create(0, 0f, 0.0, "", (0f, 0f));
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Decon()
    {
        var e = _entities[0];
        for(int i = 0; i < 1000; i++)
        {
            e.Deconstruct(out Ref<int> @int, out Ref<double> @double, out Ref<int> @int1, out Ref<double> @double1, out Ref<int> @int2);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Get()
    {
        var e = _entities[0];
        for (int i = 0; i < 1000; i++)
        {
            e.Get<int>();
        }
    }

    /*
    #region Create
    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create1()
    {
        using World w = new();
        w.EnsureCapacity<int>(100_000);
        for (int i = 0; i < 100_000; i++)
            w.Create<int>(default);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create2()
    {
        using World w = new();
        w.EnsureCapacity<int>(100_000);
        for (int i = 0; i < 100_000; i++)
            w.Create<int, int>(default, default);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Create)]
    public void Create3()
    {
        using World w = new();
        w.EnsureCapacity<int>(100_000);
        for (int i = 0; i < 100_000; i++)
            w.Create<int, int, int>(default, default, default);
    }
    #endregion

    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Has()
    {
        //_entity.Has<int>();
    }
    */
}