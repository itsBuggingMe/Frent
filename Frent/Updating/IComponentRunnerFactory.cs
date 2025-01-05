namespace Frent.Updating;
public interface IComponentRunnerFactory
{
    public object Create();
}

internal interface IComponentRunnerFactory<T>
{
    public IComponentRunner<T> CreateStronglyTyped();
}