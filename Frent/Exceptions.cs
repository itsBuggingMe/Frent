using System.Runtime.CompilerServices;

namespace Frent;

internal class FrentExceptions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Throw_InvalidOperationException<T>(string message)
    {
        throw new InvalidOperationException(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw_ComponentNotFoundException<T>()
    {
        throw new ComponentNotFoundException<T>();
    }
}

internal class ComponentNotFoundException<T>() : Exception($"Component of type {typeof(T).FullName} not found");
internal class ComponentNotFoundException(Type t) : Exception($"Component of type {t.FullName} not found");