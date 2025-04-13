namespace Frent;

/// <summary>
/// Tag struct that indicates an entity is a deferred creation entity. This means it was created during a world update and will have its components added on afterwards.
/// </summary>
[Obsolete("Unused. Entity creation during updates is fully supported.")]
public struct DeferredCreate;