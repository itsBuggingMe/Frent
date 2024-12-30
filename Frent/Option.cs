using Frent.Components;

namespace Frent;

public ref struct Option<T>(bool exists, ref T value)
    where T : IComponent
{
    public readonly bool Exists = exists;
    private ref T _value = ref value;
    public ref T Component
    {
        get
        {
            if (!Exists)
                FrentExceptions.Throw_InvalidOperationException<T>("Option has no value");

            return ref _value;
        }
    }

    public bool SetIfExists(in T value)
    {
        if(Exists)
        {
            _value = value;
        }

        return Exists;
    }
}
