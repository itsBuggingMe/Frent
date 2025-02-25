using Frent.Collections;
using Frent.Core;

namespace Frent.Updating;

internal class WorldUpdateFilter
{
    internal FastStack<ComponentID> Stack = FastStack<ComponentID>.Create(8);
    internal int NextComponentIndex;
}
