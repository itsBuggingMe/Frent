using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent;

[StackTraceHidden]
internal class FrentExceptions
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentNotFoundException(Type t)
    {
        throw new ComponentNotFoundException(t);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentNotFoundException<T>()
    {
        throw new ComponentNotFoundException(typeof(T));
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentNotFoundException(string message)
    {
        throw new ComponentNotFoundException(message);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentAlreadyExistsException(Type t)
    {
        throw new ComponentAlreadyExistsException(t);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentAlreadyExistsException(string message)
    {
        throw new ComponentAlreadyExistsException(message);
    }
}

internal class ComponentAlreadyExistsException : Exception
{
    public ComponentAlreadyExistsException(Type t)
        : base($"Component of type {t.FullName} already exists on entity!") { }

    public ComponentAlreadyExistsException(string message)
        : base(message) { }
}

internal class ComponentNotFoundException : Exception
{
    public ComponentNotFoundException(Type t)
        : base($"Component of type {t.FullName} not found") { }

    public ComponentNotFoundException(string message)
        : base(message) { }
}
