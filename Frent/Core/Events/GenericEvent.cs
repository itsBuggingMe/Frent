using Frent.Collections;

namespace Frent.Core.Events;

/// <summary>
/// A collection of <see cref="IGenericAction{Entity}"/> instances which can be added to or removed from. 
/// </summary>
public class GenericEvent
{
    internal GenericEvent() { }

    internal bool HasListeners => _first is not null;

    private IGenericAction<Entity>? _first;
    private FrugalStack<IGenericAction<Entity>> _invokationList = new FrugalStack<IGenericAction<Entity>>();

    internal void Add(IGenericAction<Entity> action)
    {
        if (_first is null)
        {
            _first = action;
        }
        else
        {
            _invokationList.Push(action);
        }
    }

    internal void Remove(IGenericAction<Entity> action)
    {
        if (_first == action)
        {
            _first = null;
            if (_invokationList.TryPop(out var v))
                _first = v;
        }
        else
        {
            _invokationList.Remove(action);
        }
    }

    internal void Invoke<T>(Entity entity, ref T arg)
    {
        if (_first is not null)
        {
            _first.Invoke(entity, ref arg);
            foreach (var item in _invokationList.AsSpan())
                item.Invoke(entity, ref arg);
        }
    }


    //https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads
    //I don't think its violating "DO NOT be cute when defining operator overloads." since its what event does.
    /// <summary>
    /// Adds an <see cref="IGenericAction{Entity}"/> to this event instance
    /// </summary>
    /// <param name="left">The event collection to add to.</param>
    /// <param name="right">The event to add</param>
    /// <returns>The event itself. When <paramref name="left"/> is null, the return value is also null.</returns>
    public static GenericEvent? operator +(GenericEvent? left, IGenericAction<Entity> right)
    {
        if (left is null)
            return null;

        if (left._first is null)
        {
            left._first = right;
        }
        else
        {
            left._invokationList.Push(right);
        }
        return left;
    }

    /// <summary>
    /// Unsubscribes an <see cref="IGenericAction{Entity}"/> to this event instance
    /// </summary>
    /// <param name="left">The event collection to unsubscribe from.</param>
    /// <param name="right">The event to unsubscribe</param>
    /// <returns>The event itself. When <paramref name="left"/> is null, the return value is also null.</returns>
    public static GenericEvent? operator -(GenericEvent? left, IGenericAction<Entity> right)
    {
        if (left is null)
            return null;

        left.Remove(right);
        return left;
    }
}