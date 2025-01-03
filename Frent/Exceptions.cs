using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent;

internal class FrentExceptions
{
    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }
    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Throw_InvalidOperationException<T>(string message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentNotFoundException<T>()
    {
        throw new ComponentNotFoundException<T>();
    }

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentAlreadyExistsException<T>()
    {
        throw new ComponentNotFoundException<T>();
    }
}

internal class ComponentAlreadyExistsException<T> : Exception
{
    public ComponentAlreadyExistsException() : base($"Component of type {typeof(T).FullName} already exists on entity!")
    {

    }
}
internal class ComponentNotFoundException<T> : Exception
{
    public ComponentNotFoundException() : base($"Component of type {typeof(T).FullName} not found")
    {

    }
}
internal class ComponentNotFoundException : Exception
{
    public ComponentNotFoundException(Type t) : base($"Component of type {t.FullName} not found")
    {

    }
}