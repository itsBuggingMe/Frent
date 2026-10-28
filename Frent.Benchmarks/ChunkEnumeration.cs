using BenchmarkDotNet.Attributes;
using Frent;

namespace Frent.Benchmarks;

internal struct BenchPosition
{
    public float X;
}

internal struct BenchVelocity
{
    public float X;
}

internal class ChunkEnumeration
{
    private World World;

    [GlobalSetup]
    public void Setup()
    {
        World = new World();
        for (int i = 0; i < 100_000; i++)
            World.Create(new BenchPosition { X = i }, new BenchVelocity { X = 1 });
    }

    [Benchmark(Baseline = true)]
    public long ChunkSpans()
    {
        long acc = 0;
        foreach (var chunk in World.Query<BenchPosition, BenchVelocity>().EnumerateChunks<BenchPosition, BenchVelocity>())
        {
            chunk.Deconstruct(out var positions, out var velocities);
            for (int i = 0; i < positions.Length; i++)
                acc += (int)positions[i].X;
        }
        return acc;
    }

    [Benchmark]
    public long ChunkEntities()
    {
        long acc = 0;
        foreach (var chunk in World.Query<BenchPosition, BenchVelocity>().EnumerateChunks<BenchPosition, BenchVelocity>())
        {
            chunk.Deconstruct(out var positions, out var velocities);
            var entities = chunk.Entities;
            for (int i = 0; i < positions.Length; i++)
                acc += (int)positions[i].X + entities[i].GetHashCode();
        }
        return acc;
    }

    [Benchmark]
    public long EnumerateWithEntities()
    {
        long acc = 0;
        foreach (var (entity, position, velocity) in World.Query<BenchPosition, BenchVelocity>().EnumerateWithEntities<BenchPosition, BenchVelocity>())
            acc += (int)position.Value.X + entity.GetHashCode();
        return acc;
    }
}
