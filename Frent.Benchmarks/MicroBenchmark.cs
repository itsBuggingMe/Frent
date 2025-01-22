using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Frent.Benchmarks.Program;
using Frent.Systems;
using Frent.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entity = _world.Create<int, double>(default, default);
    }

    int x;
    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Opt()
    {

    }

    [Benchmark]
    [BenchmarkCategory(Categories.Has)]
    public void Norm()
    {

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