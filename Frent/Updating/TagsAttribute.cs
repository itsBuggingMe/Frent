namespace Frent.Updating;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IncludesTagsAttribute(params Type[] types) : Attribute
{
    public Type[] Includes { get; } = types;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExcludesTagsAttribute(params Type[] types) : Attribute
{
    public Type[] Excludes { get; } = types;
}