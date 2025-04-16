using Frent.Collections;

namespace Frent.Core;

internal record struct ComponentData(Type Type, IDTable Storage, Delegate? Initer, Delegate? Destroyer, bool IsSparseComponent);