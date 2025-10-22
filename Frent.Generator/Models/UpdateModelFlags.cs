using System;

namespace Frent.Generator.Models;

[Flags]
internal enum UpdateModelFlags
{
    None = 0,
    IsClass = 1 << 0,
    IsStruct = 1 << 1,
    IsGeneric = 1 << 2,
    Initable = 1 << 3,
    Destroyable = 1 << 4,
    IsRecord = 1 << 5,
    IsSelfInit = 1 << 6,
    IsSparse = 1 << 7,
    IsTag = 1 << 8,
}
