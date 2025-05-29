using Frent.Core;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

internal interface IRunner
{
    internal void Run(Array buffer, Archetype b, World world, int start, int length);
    internal void Run(Array buffer, Archetype b, World world);

    public static ref T GetComponentStorageDataReference<T>(Array array)
    {
        return ref MemoryMarshal.GetArrayDataReference(UnsafeExtensions.UnsafeCast<T[]>(array));
    }
}