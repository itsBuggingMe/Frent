using Frent.Components;

namespace Frent.Serialization;

public interface IOnDeserialize : IComponentBase
{
    public void OnDeserialize(Entity self);
}