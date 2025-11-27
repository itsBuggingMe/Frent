using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Frent.Updating.AttributeUpdateFilter;

namespace Frent;

[StackTraceHidden]
internal class FrentExceptions
{
    [DoesNotReturn]
    public static void Throw_ArgumentException(string message)
    {
        throw new ArgumentException(message);
    }

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

    internal static MissingComponentException? CreateExceptionArchetype(World world, Archetype archetype, Span<ArchetypeUpdateMethod> methodsToCheck)
    {
        // this can be slow - failure path

        if (archetype.EntityCount == 0)
            return null;

        Span<EntityIDOnly> entities = archetype.GetEntitySpan();

        foreach (ref ArchetypeUpdateMethod potentialFailure in methodsToCheck)
        {
            // ComponentID -> potentialFailure.Index
            byte[] indexTable = archetype.ComponentTagTable;

            for (int i = 0; i < indexTable.Length; i++)
            {
                // find backwards from storage index to component id
                if ((indexTable[i] & GlobalWorldTables.IndexBits) == potentialFailure.Index)
                {
                    ComponentID failedComponent = new((ushort)i);

                    // loop through depdendencies of this component to see if any are missing
                    UpdateMethodData metadata = failedComponent.Methods[potentialFailure.MetadataIndex];

                    foreach (Type shouldHaveThisComponent in metadata.Dependencies)
                    {
                        ComponentID id = Component.GetComponentID(shouldHaveThisComponent);
                        foreach (var entity in entities)
                        {
                            if (!entity.ToEntity(world).Has(id))
                            {
                                return new MissingComponentException(failedComponent.Type, id.Type, entity.ToEntity(world));
                            }
                        }
                    }

                    break;
                }
            }
        }

        // everything in order, must be user null reference exception
        return null;
    }

    internal static MissingComponentException? CreateExceptionSparse(World world, ComponentSparseSetBase sparseSet, int entityId, Func<UpdateMethodData, bool> filter)
    {
        ComponentID componentId = Component.GetComponentID(sparseSet.Type);

        Entity e = new Entity(world.WorldID, world.EntityTable[entityId].Version, entityId);

        Debug.Assert(world.EntityTable[entityId].Archetype is not null);

        foreach (var method in componentId.Methods)
        {
            if (filter(method))
            {
                foreach (var dep in method.Dependencies)
                {
                    if (!e.Has(dep))
                        return new MissingComponentException(componentId.Type, dep, e);
                }
            }
        }

        return null;
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
    : Exception($"Entity {invalidEntity.EntityID} with component {componentType.Name} missing dependency {expectedType.Name}.")
{
    /// <summary>
    /// The dependent component type that caused the exception.
    /// </summary>
    public Type ComponentType { get; } = componentType;
    /// <summary>
    /// The component dependency that is missing.
    /// </summary>
    public Type MissingComponent { get; } = expectedType;
    /// <summary>
    /// The entity on which <see cref="ComponentType"/> exists but <see cref="MissingComponent"/> is missing.
    /// </summary>
    public Entity InvalidEntity { get; } = invalidEntity;
}