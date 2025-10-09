namespace Frent.Core;

/// <summary>
/// An generic action with known parameter
/// </summary>
/// <remarks>Since delegates cannot be unbound generics, we use an interface instead</remarks>
/// <typeparam name="TParam">The first parameter, which is normally bound</typeparam>
public interface IGenericAction<TParam>
{
    /// <summary>
    /// Runs the arbitrary generic method that this <see cref="IGenericAction{TParam}"/> represents
    /// </summary>
    /// <typeparam name="T">The unbound generic parameter</typeparam>
    /// <param name="param">The first parameter</param>
    /// <param name="type">The generic parameter</param>
    public void Invoke<T>(TParam param, ref T type);
}

/// <summary>
/// An generic action with known parameter
/// </summary>
/// <remarks>Since delegates cannot be unbound generics, we use an interface instead</remarks>
public interface IGenericAction
{
    /// <summary>
    /// Runs the arbitrary generic method that this <see cref="IGenericAction"/> represents
    /// </summary>
    /// <typeparam name="T">The unbound generic parameter</typeparam>
    /// <param name="type">The generic parameter</param>
    public void Invoke<T>(ref T type);
}