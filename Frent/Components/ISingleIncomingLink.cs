namespace Frent.Components;

/// <summary>
/// Indicates a type is intended as a link that an entity can have only one incoming link of.
/// </summary>
/// <remarks>Types with this tag will be automatically registered for serialization.</remarks>
public interface ISingleIncomingLink : ILink;