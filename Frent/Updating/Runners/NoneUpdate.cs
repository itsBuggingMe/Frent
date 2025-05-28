using Frent.Core;

namespace Frent.Updating.Runners;

internal class NoneUpdate<TComp> : IRunner
{
    public void Run(Array buffer, Archetype b, World world, int start, int length) { }
    public void Run(Array buffer, Archetype b, World world) { }
}
