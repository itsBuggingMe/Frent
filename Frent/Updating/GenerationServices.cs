using Frent.Components;
using Frent.Core;
using Frent.Updating.Runners;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Frent.Updating;

/// <summary>
/// Used only for source generation
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenerationServices
{
    // Component Type -> Runner methods
    internal static readonly Dictionary<Type, IRunner[]> UserGeneratedTypeMap = new();
    // Runner -> Attributes
    internal static readonly Dictionary<IRunner, Type[]> MethodAttributes = new();
    internal static readonly Dictionary<Type, Delegate> TypeIniters = new();
    internal static readonly Dictionary<Type, Delegate> TypeDestroyers = new();
    internal static readonly Dictionary<Type, ComponentBufferManager> ComponentFactories = [];

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
        ComponentFactories[typeof(T)] = new ComponentBufferManager<T>();
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterUpdateType(Type type, params UpdateMethod[] methods)
    {
        if (!UserGeneratedTypeMap.TryGetValue(type, out var val))
        {
            IRunner[] runnerArray = new IRunner[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Runner is not IRunner runner)
                    throw new InvalidOperationException($"Object given that is not a runner. Source generation may be broken. This method should not be called from user code!");

                runnerArray[i] = runner;
                MethodAttributes.Add(runner, methods[i].Attributes);
            }
            UserGeneratedTypeMap.Add(type, runnerArray);
            return;
        }
        throw new InvalidOperationException("Source generation appears to be broken. This method should not be called from user code!");
    }
}

/// <inheritdoc cref="GenerationServices"/>
public readonly record struct UpdateMethod(object Runner, Type[] Attributes);