namespace Frent.Collections;

internal class IDTable<T>
{
    public ref T this[int index]
    {
        get => ref _buffer.AsSpan()[index];
    }

    private FastStack<int> _recycled = FastStack<int>.Create(2);
    private Table<T> _buffer = new Table<T>(4);
    private int _index;

    public ref T Create(out int id)
    {
        return ref _buffer[id = _index++];
    }
}
