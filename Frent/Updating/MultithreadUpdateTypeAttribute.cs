namespace Frent.Updating;
/// <summary>
/// Update type attributes that extend this will be mulithreaded when called using this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class MultithreadUpdateTypeAttribute : UpdateTypeAttribute;