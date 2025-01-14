using System.Runtime.InteropServices;

namespace Frent.Updating;

/// <summary>
/// Used only for source generation
/// </summary>
public static class GenerationServices
{
    internal static readonly Dictionary<Type, IComponentRunnerFactory> UserGeneratedTypeMap = new();
    internal static readonly Dictionary<Type, HashSet<Type>> TypeAttributeCache = new();

    /// <summary>
    /// Used only for source generation
    /// </summary>
    public static void RegisterType(Type type, IComponentRunnerFactory value)
    {
        if (UserGeneratedTypeMap.TryGetValue(type, out IComponentRunnerFactory? val))
        {
            if (val.GetType() != value.GetType())
            {
                throw new Exception($"Attempted to initalize {type.FullName} with {val.GetType().FullName} and {value.GetType().FullName}");
            }
        }
        else
        {
            UserGeneratedTypeMap.Add(type, value);
        }
    }

    public static void RegisterUpdateMethodAttribute(Type attributeType, Type componentType)
    {
        (CollectionsMarshal.GetValueRefOrAddDefault(TypeAttributeCache, attributeType, out _) ??= new()).Add(componentType);
    }
}
