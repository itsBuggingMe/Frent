namespace Frent.Updating;

public static class GenerationServices
{
    internal static readonly Dictionary<Type, IComponentRunner> UserGeneratedTypeMap = new();

    public static void RegisterType(Type type, IComponentRunner value)
    {
        if (UserGeneratedTypeMap.TryGetValue(type, out IComponentRunner? val))
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
}
