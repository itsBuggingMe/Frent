using Frent.Core;

namespace Frent.Updating.Runners;

internal class NoneUpdate<TComp> : IRunner
{
    public void Run(Array array, Archetype b, World world) { }
    public void Run(Array array, Archetype b, World world, int start, int length) { }
}