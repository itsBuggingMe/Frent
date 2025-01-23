using Frent.Collections;
using Frent.Components;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Diagnostics.CodeAnalysis;

namespace Frent.Core;

public enum MemoryTrimming
{
    Always = 0,
    Normal = 1,
    Never = 2,
}