using Frent.Buffers;
using Frent.Components;
using Frent.Core;

namespace Frent.Updating.Runners;
public class None<TComp> : ComponentRunnerBase<None<TComp>, TComp>
{
    public override void Run(Archetype b) { }
}