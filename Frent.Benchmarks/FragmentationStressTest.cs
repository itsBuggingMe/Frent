using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Frent.Components;
using Frent.Core;
using Frent.Updating;

namespace Frent.Benchmarks;

public class FragmentationStressTest
{
    private World _world;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        ComponentHandle[] handles = [
            ComponentHandle.Create<C1>(default),
            ComponentHandle.Create<C2>(default),
            ComponentHandle.Create<C3>(default),
            ComponentHandle.Create<C4>(default),
            ComponentHandle.Create<C5>(default),
        ];

        for (int i = 0; i < 1000; i++)
        {
            int count = Random.Shared.Next(6);
            Random.Shared.Shuffle(handles);

            _world.CreateFromHandles(handles.AsSpan(0, count));
        }
    }


    [Benchmark]
    public void Fragmented()
    {
        _world.Update<TickAttribute>();
    }

    [StructLayout((ushort)0, Size = 16)]
    internal record struct C1 : IComponent<C1>
    {
        [Tick]
        public void Update(ref C1 arg) { }
    }

    [StructLayout((ushort)0, Size = 16)]
    internal record struct C2 : IComponent<C2>
    {
        [Tick]
        public void Update(ref C2 arg) { }
    }

    [StructLayout((ushort)0, Size = 16)]
    internal record struct C3 : IComponent<C3>
    {
        [Tick]
        public void Update(ref C3 arg) { }
    }

    [StructLayout((ushort)0, Size = 16)]
    internal record struct C4 : IComponent<C4>
    {
        [Tick]
        public void Update(ref C4 arg) { }
    }

    [StructLayout((ushort)0, Size = 16)]
    internal record struct C5 : IComponent<C5>
    {
        [Tick]
        public void Update(ref C5 arg) { }
    }
    internal class TickAttribute : UpdateTypeAttribute;
}
