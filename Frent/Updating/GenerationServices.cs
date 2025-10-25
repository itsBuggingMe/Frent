using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Serialization;
using Frent.Updating.Runners;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Frent.Updating;

/// <summary>
/// Used only for source generation
/// </summary>
/// <remarks>
/// Should not be manually called from user code. Misuse could result in unexpected behavior.
/// </remarks>
/// <variadic />
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenerationServices
{
    // Component Type -> Runner methods
    internal static readonly RefDictionary<Type, UpdateMethodData[]> UserGeneratedTypeMap = new();
    internal static readonly Dictionary<Type, Delegate> TypeIniters = new();
    internal static readonly Dictionary<Type, Delegate> TypeDestroyers = new();
    internal static readonly Dictionary<Type, Delegate> TypeSerialize = new();
    internal static readonly Dictionary<Type, Delegate> TypeDeserialize = new();
    internal static readonly List<IJsonTypeInfoResolver> GeneratedJsonTypeInfoResolvers = new();

    // string name for deserialization
    internal static readonly Dictionary<string, Type> SerializableTypesMap = new();

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
    public static void RegisterSerialize<T>()
        where T : IOnSerialize
    {
        TypeSerialize[typeof(T)] = (ComponentDelegates<T>.OnSerialize)([method: DebuggerHidden, DebuggerStepThrough] static (ref T c) => c.OnSerialize());
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterDeserialize<T>()
        where T : IOnDeserialize
    {
        TypeDeserialize[typeof(T)] = (ComponentDelegates<T>.OnDeserialize)([method: DebuggerHidden, DebuggerStepThrough] static (Entity e, ref T c) => c.OnDeserialize(e));
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterComponent<T>()
    {
        Core.Component.CachedComponentFactories.TryAdd(typeof(T), new ComponentBufferManager<T>());
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterJsonTypeInfoResolver(IJsonTypeInfoResolver resolver)
    {
        GeneratedJsonTypeInfoResolvers.Add(resolver);
    }

    /// <inheritdoc cref="GenerationServices"/>
    public static void RegisterUpdateType(Type type, params UpdateMethodData[] methods)
    {
        if (methods.Length > 64)
            FrentExceptions.Throw_InvalidOperationException("Components cannot have more than 64 update methods.");

        ref UpdateMethodData[]? data = ref UserGeneratedTypeMap.GetValueRefOrAddDefault(type, out bool exists);
        if (!exists)
        {
            data = methods;
        }
    }
}

/// <inheritdoc cref="GenerationServices"/>
public readonly struct UpdateMethodData(IRunner Runner, Type[] Attributes, TypeFilterRecord TypeFilterRecord)
{
    internal readonly IRunner Runner = Runner;
    internal readonly Type[] Attributes = Attributes;
    internal readonly TypeFilterRecord TypeFilterRecord = TypeFilterRecord;

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

/// <inheritdoc cref="GenerationServices"/>
public record struct TypeFilterRecord(Type[] IncludeComponents, Type[] ExcludeComponents, Type[] IncludeTags, Type[] ExcludeTags)
{
    /// <inheritdoc cref="GenerationServices"/>
    public static readonly TypeFilterRecord None = new([], [], [], []);
}