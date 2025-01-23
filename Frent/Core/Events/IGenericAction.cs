namespace Frent.Core;
public interface IGenericAction<TParam>
{
    public void Invoke<T>(TParam param, T type);
}
