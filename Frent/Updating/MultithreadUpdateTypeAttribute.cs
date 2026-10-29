using Frent.Core;

namespace Frent.Updating;
/// <summary>
/// Update type attributes that extend this will be mulithreaded when called using this attribute. Note that <see cref="World"/> and <see cref="CommandBuffer"/> are not thread safe.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class MultithreadUpdateTypeAttribute : UpdateTypeAttribute;