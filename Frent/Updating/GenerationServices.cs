using Frent.Components;
using Frent.Core;
using Frent.Updating.Runners;
using System.ComponentModel;
using System.Diagnostics;

namespace Frent.Updating;

/// <summary>
/// Used only for source generation
/// </summary>
/// <variadic />
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenerationServices
{
    // Component Type -> Runner methods
    internal static readonly Dictionary<Type, UpdateMethodData[]> UserGeneratedTypeMap = new();
    internal static readonly Dictionary<Type, Delegate> TypeIniters = new();
    internal static readonly Dictionary<Type, Delegate> TypeDestroyers = new();

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterInit<T>()
        where T : IInitable
    {
        TypeIniters[typeof(T)] = (ComponentDelegates<T>.InitDelegate)([method: DebuggerHidden, DebuggerStepThrough] static (Entity e, ref T c) => c.Init(e));
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterDestroy<T>()
        where T : IDestroyable
    {
        TypeDestroyers[typeof(T)] = (ComponentDelegates<T>.DestroyDelegate)([method: DebuggerHidden, DebuggerStepThrough] static (ref T c) => c.Destroy());
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterComponent<T>()
    {
        Core.Component.CachedComponentFactories.TryAdd(typeof(T), new ComponentBufferManager<T>());
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterUpdateType(Type type, params UpdateMethodData[] methods)
    {
        if (methods.Length > 64)
            FrentExceptions.Throw_InvalidOperationException("Components cannot have more than 64 update methods.");

        if (!UserGeneratedTypeMap.TryGetValue(type, out var val))
        {
            UserGeneratedTypeMap.Add(type, methods);
            return;
        }
        throw new InvalidOperationException("Source generation appears to be broken. This method should not be called from user code!");
    }
}

/// <inheritdoc cref="GenerationServices"/>
public readonly record struct UpdateMethodData(IRunner Runner, Type[] Attributes)
{
    internal readonly bool AttributeIsDefined(Type attributeType)
    {
        foreach (var attr in Attributes)
        {
            if (attr == attributeType)
                return true;
        }
        return false;
    }
}