namespace Frent.Serialization;

/// <summary>
/// Marks an implementation of <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> as being used for component serialization.
/// </summary>
/// <remarks>Use in conjunction with the <see cref="System.Text.Json.Serialization.JsonSerializableAttribute"/> attribute.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ComponentJsonSerializerContextAttribute : Attribute;