using Frent.Updating;

namespace Frent.Core;

public abstract class Box
{
    public abstract Type Type { get; }
    public abstract T Get<T>();
    internal abstract void CopyInto(IComponentRunner componentRunner, ushort chunkIndex, ushort componentIndex);
}

public class Box<T> : Box
    where T : notnull
{
    public Box(T value) => _value = value;
    public override Type Type => typeof(T);
    public override T1 Get<T1>() => (T1)(object)_value;

    internal override void CopyInto(IComponentRunner componentRunner, ushort chunkIndex, ushort componentIndex)
    {
        IComponentRunner<T> typed = (IComponentRunner<T>)componentRunner;
        typed.AsSpan()[chunkIndex][componentIndex] = _value;
    }

    public T Value => _value;
    public static implicit operator T(Box<T> box) => box.Value;
    public static implicit operator Box<T>(T value) => new(value);
    private T _value;
}