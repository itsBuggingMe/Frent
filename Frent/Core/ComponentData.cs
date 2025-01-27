using Frent.Collections;

namespace Frent.Core;

internal record struct ComponentData(Type Type, TrimmableStack Stack, int updateOrder);