using Frent.Components;
using System.Runtime.CompilerServices;

namespace Frent.Benchmarks;
internal record struct Component32(int Value) : IEntityUpdateComponent
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Update(Entity e)
    {

    }
}

internal record struct Component64(long Value);
internal record struct Component128(long Value1, long Value2);