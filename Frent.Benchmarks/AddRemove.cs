using BenchmarkDotNet.Attributes;
using Frent.Components;

namespace Frent.Benchmarks;


internal partial class AddRemove
{
    private Entity[] _entities;
    private World World;
    [GlobalSetup]
    public void Setup()
    {
        _entities = new Entity[100_000];
        World = new World();
        for (int i = 0; i < _entities.Length; i++)
        {
            _entities[i] = World.Create();
        }
    }

    [Benchmark]
    public void Arch()
    {
        foreach (var entity in _entities)
            entity.Add(default(ArchComp));
        foreach (var entity in _entities)
            entity.Remove<ArchComp>();
    }

    [Benchmark]
    public void Sparse()
    {
        foreach (var entity in _entities)
            entity.Add(default(SparseComp));
        foreach (var entity in _entities)
            entity.Remove<SparseComp>();
    }

    partial struct SparseComp : ISparseComponent;
    partial struct ArchComp;
}
