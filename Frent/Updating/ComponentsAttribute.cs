namespace Frent.Updating;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IncludesComponentsAttribute(params Type[] types) : Attribute
{
    public Type[] Includes { get; } = types;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExcludesComponentsAttribute(params Type[] types) : Attribute
{
    public Type[] Excludes { get; } = types;
}