namespace Frent;

public class DefaultUniformProvider : IUniformProvider
{
    private Dictionary<Type, object> _uniforms = [];

    public DefaultUniformProvider Add<T>(T obj)
        where T : notnull
    {
        _uniforms[typeof(T)] = obj;
        return this;
    }

    public DefaultUniformProvider Add(Type type, object @object)
    {
        ArgumentNullException.ThrowIfNull(type);
        _uniforms[type] = @object;
        return this;
    }

    public T GetUniform<T>() => _uniforms.TryGetValue(typeof(T), out object? value) ?
        (T)value :
        throw new InvalidOperationException($"Uniform of {typeof(T).Name} not found");
}
