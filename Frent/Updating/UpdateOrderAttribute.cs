namespace Frent.Updating;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
[Obsolete("Unused")]
#pragma warning disable CS9113 // Parameter is unread.
internal class UpdateOrderAttribute(int order) : Attribute, IComponentUpdateOrderAttribute;
#pragma warning restore CS9113 // Parameter is unread.
/// <summary>
/// Marker interface for any update order attribute.
/// </summary>
/// <remarks>This is an interface so the user can implement an 
/// <see cref="UpdateTypeAttribute"/> as also an order attribute</remarks>
[Obsolete("Unused")]
internal interface IComponentUpdateOrderAttribute;