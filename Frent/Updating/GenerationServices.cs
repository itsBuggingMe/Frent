using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating;

/// <summary>
/// Used only for source generation
/// </summary>
public static class GenerationServices
{
    internal static readonly Dictionary<Type, (IComponentRunnerFactory Factory, int UpdateOrder)> UserGeneratedTypeMap = new();
    internal static readonly Dictionary<Type, HashSet<Type>> TypeAttributeCache = new();

    /// <summary>
    /// Used only for source generation
    /// </summary>
    public static void RegisterType(Type type, IComponentRunnerFactory value, int updateOrder = 0)
    {
        if (UserGeneratedTypeMap.TryGetValue(type, out var val))
        {
            if (val.Factory.GetType() != value.GetType())
            {
                throw new Exception($"Attempted to initalize {type.FullName} with {val.GetType().FullName} and {value.GetType().FullName}");
            }
        }
        else
        {
            UserGeneratedTypeMap.Add(type, (value, updateOrder));
        }
    }

    /// <summary>
    /// Used only for source generation
    /// </summary>
    public static void RegisterUpdateMethodAttribute(Type attributeType, Type componentType)
    {
        (CollectionsMarshal.GetValueRefOrAddDefault(TypeAttributeCache, attributeType, out _) ??= []).Add(componentType);
    }
}
