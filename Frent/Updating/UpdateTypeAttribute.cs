namespace Frent.Updating;
/// <summary>
/// The base class of all attributes used to filter world updates
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public abstract class UpdateTypeAttribute : Attribute;