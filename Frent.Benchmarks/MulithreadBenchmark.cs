using BenchmarkDotNet.Attributes;
using Frent.Components;
using Frent.Updating;
using System;
using System.Runtime.CompilerServices;

namespace Frent.Benchmarks;

public class MultithreadBenchmark
{
    //[Params(1_000, 10_000, 100_000, 1_000_000)]
    public int EntityCount { get; set; } = 1_000;


    private World _world;
    
    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        for(int i = 0; i < EntityCount; i++)
        {
            _world.Create(default(SomeComponent), 1);
        }
        _world.Update<Singlethread>();
        _world.Update<Multithread>();
    }

    [Benchmark]
    public void Single()
    {
        _world.Update<Singlethread>();
    }

    [Benchmark]
    public void Multi()
    {
        _world.Update<Multithread>();
    }
}

public struct SomeComponent : IUpdate<int>
{
    public InlineArray16 Buffer;

    [Multithread]
    [Singlethread]
    public void Update(ref int arg)
    {
        foreach (ref var value in Buffer)
        {
            value += arg;
        }
    }
}

internal class Multithread : MultithreadUpdateTypeAttribute;
internal class Singlethread : UpdateTypeAttribute;

[InlineArray(16)]
public struct InlineArray16
{
    int _0;
}