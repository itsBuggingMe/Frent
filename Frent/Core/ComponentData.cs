using Frent.Collections;
using Frent.Updating;

namespace Frent.Core;

internal record struct ComponentData(Type Type, IDTable Storage, Delegate? Initer, Delegate? Destroyer, UpdateMethodData[] UpdateMethods, bool IsSparseComponent);