namespace Frent.Sample.Asteroids.Editor;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
internal class EditorAttribute : Attribute;
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class)]
internal class DescriptionAttribute(string description) : Attribute
{
    public string Description { get; set; } = description;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class EditorExclude : Attribute;