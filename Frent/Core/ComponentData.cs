using Frent.Collections;
using Frent.Updating;

namespace Frent.Core;

/// <remarks>
/// SparseComponentIndex == 0 when not a sparse component, n where WorldSparseSetTable[n] is of type <see paramref="Type"/>
/// </remarks>
internal record struct ComponentData(Type Type, IDTable Storage, Delegate? Initer, Delegate? Destroyer, UpdateMethodData[] UpdateMethods, int SparseComponentIndex);