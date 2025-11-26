using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Frent;

[StackTraceHidden]
internal class FrentExceptions
{
    [DoesNotReturn]
    public static void Throw_InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    public static void Throw_ComponentNotFoundException(Type t)
    {
        throw new ComponentNotFoundException(t);
    }

    [DoesNotReturn]
    public static void Throw_ComponentNotFoundException<T>()
    {
        throw new ComponentNotFoundException(typeof(T));
    }

    [DoesNotReturn]
    public static void Throw_ComponentAlreadyExistsException(Type t)
    {
        throw new ComponentAlreadyExistsException(t);
    }

    [DoesNotReturn]
    public static void Throw_ComponentAlreadyExistsException<T>()
    {
        throw new ComponentAlreadyExistsException(typeof(T));
    }

    [DoesNotReturn]
    public static void Throw_ArgumentOutOfRangeException(string message)
    {
        throw new ArgumentOutOfRangeException(message);
    }

    [DoesNotReturn]
    public static void Throw_NullReferenceException()
    {
        throw new NullReferenceException();
    }
}

/// <summary>
/// Thrown when a component already exists on an entity.
/// </summary>
public class ComponentAlreadyExistsException(Type t) : Exception($"Component of type {t.FullName} already exists on entity!");

/// <summary>
/// Represents an exception that is thrown when a requested component type cannot be found.
/// </summary>
public class ComponentNotFoundException(Type t) : Exception($"Component of type {t.FullName} not found");

/// <summary>
/// An exception that is thrown when an entity is missing a required component during an update.
/// </summary>
public class MissingComponentException(Type componentType, Type expectedType, Entity invalidEntity)
    : Exception($"Entity {invalidEntity.EntityID} from world {invalidEntity.WorldID} with component {componentType.Name} missing dependency {expectedType.Name}.")
{
    /// <summary>
    /// The dependent component type that caused the exception.
    /// </summary>
    public Type ComponentType { get; } = componentType;
    /// <summary>
    /// The component dependency that is missing.
    /// </summary>
    public Type ExpectedType { get; } = expectedType;
    /// <summary>
    /// The entity on which <see cref="ComponentType"/> exists but <see cref="ExpectedType"/> is missing.
    /// </summary>
    public Entity InvalidEntity { get; } = invalidEntity;
}