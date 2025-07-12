using Frent.Core;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public interface IRunner
{
    internal ComponentID ComponentID { get; }

    internal void Run(Array buffer, Archetype b, World world, int start, int length);
    internal void Run(Array buffer, Archetype b, World world);
    internal static ref T GetComponentStorageDataReference<T>(Array array)
    {
        return ref MemoryMarshal.GetArrayDataReference(UnsafeExtensions.UnsafeCast<T[]>(array));
    }
}