using Frent.Components;

namespace Frent.Serialization;

public interface IOnSerialize : IComponentBase
{
    public void OnSerialize();
}