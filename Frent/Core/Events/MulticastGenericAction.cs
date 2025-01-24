using System.Collections;

namespace Frent.Core.Events;

//linked list, just like `MulticastDelegate`
internal class MulticastGenericAction<T> : IGenericAction<T>, IEnumerable<IGenericAction<T>>
{
    private static int _nextActionID;

    private MulticastGenericAction<T>? _next;
    private IGenericAction<T>? _thisAction;
    private int _id;

    public MulticastGenericAction(IGenericAction<T>? innerAction) : 
        this(innerAction, Interlocked.Increment(ref _nextActionID))
    {

    }

    public MulticastGenericAction(IGenericAction<T>? innerAction, int id)
    {
        _thisAction = innerAction;
        _id = id;
    }

    public static MulticastGenericAction<T> Combine(MulticastGenericAction<T>? left, IGenericAction<T> right)
    {
        if(left is null)
        {
            return new MulticastGenericAction<T>(right);
        }

        if(right is MulticastGenericAction<T> anotherMultiCast && anotherMultiCast._id == left._id)
        {
            throw new ArgumentException("This action has already been added, or is itself!", nameof(right));
        }

        if (left._thisAction is null)
        {
            left._thisAction = right;
            return left;
        }

        var @new = new MulticastGenericAction<T>(right, left._id);
        @new._next = left;
        return @new;
    }

    public static MulticastGenericAction<T>? Remove(MulticastGenericAction<T>? action, IGenericAction<T> actionToRemove)
    {
        if (action is null)
            return null;

        if (action._thisAction == actionToRemove)
        {
            if (action._next != null)
            {
                action._thisAction = action._next._thisAction;
                action._next = action._next._next;
            }
            else
            {
                action._thisAction = null;
            }
            return action;
        }

        var current = action;
        while (current._next != null)
        {
            if (current._next._thisAction == actionToRemove)
            {
                current._next = current._next._next;
                return action;
            }
            current = current._next;
        }
        return action;
    }

    //https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads
    //I don't think its violating "DO NOT be cute when defining operator overloads." since its what event does.
    public static MulticastGenericAction<T> operator +(MulticastGenericAction<T>? left, IGenericAction<T> right) => Combine(left, right);
    public static MulticastGenericAction<T>? operator -(MulticastGenericAction<T>? left, IGenericAction<T> right) => Remove(left, right);

    public void Invoke<T1>(T param, T1 type)
    {
        _thisAction?.Invoke(param, type);

        var toInvoke = _next;
        while(toInvoke is not null)
        {
            toInvoke._thisAction?.Invoke(param, type);
            toInvoke = toInvoke._next;
        }
    }

    public MulticastGenericActionEnumerator GetEnumerator() => new MulticastGenericActionEnumerator(this);
    IEnumerator<IGenericAction<T>> IEnumerable<IGenericAction<T>>.GetEnumerator() => new MulticastGenericActionEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new MulticastGenericActionEnumerator(this);

    public struct MulticastGenericActionEnumerator : IEnumerator<IGenericAction<T>>
    {
        private MulticastGenericAction<T> _root;
        private MulticastGenericAction<T>? _current;

        public MulticastGenericActionEnumerator(MulticastGenericAction<T> root)
        {
            _current = root;
            _root = root;
        }

        public IGenericAction<T>? Current => _current._thisAction;

        object IEnumerator.Current => _current._thisAction;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (_current is null)
                return false;
            _current = _current._next;
            return _current is not null;
        }

        public void Reset()
        {
            _current = _root;
        }
    }
}
