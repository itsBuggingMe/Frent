namespace Frent;

/// <summary>
/// Represents the potential existence of a reference to a <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of potential inner value.</typeparam>
/// <param name="exists">Indicates if the value exists or not.</param>
/// <param name="value">The reference to <typeparamref name="T"/> to wrap around.</param>
public ref struct Option<T>(bool exists, ref T value)
{
    public readonly bool Exists = exists;

    private ref T _value = ref value;

    /// <summary>
    /// The inner wrapped reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="Option{T}"/> does not contain a value; <see cref="Exists"/> is <see langword="false"/>.</exception>
    public ref T Component
    {
        get
        {
            if (!Exists)
                FrentExceptions.Throw_InvalidOperationException<T>("Option has no value");
            return ref _value;
        }
    }

    /// <summary>
    /// Sets the inner reference to a value, only if it exists.
    /// </summary>
    /// <param name="value">The value to try set.</param>
    /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
    public bool SetIfExists(in T value)
    {
        if (Exists)
        {
            _value = value;
        }

        return Exists;
    }
}
