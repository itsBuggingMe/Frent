namespace Frent;

/// <summary>
/// Built-in tag that can be used to disable entities.
/// </summary>
/// <remarks>Entities with the <see cref="Disable"/> tag will not be updated in <see cref="World.Update()"/> or similar overloads, nor in queries unless explicitly required.</remarks>
public struct Disable;