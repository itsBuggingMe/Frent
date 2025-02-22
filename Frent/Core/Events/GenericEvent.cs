﻿using Frent.Collections;

namespace Frent.Core.Events;

public class GenericEvent
{
    internal GenericEvent() { }

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
    public static GenericEvent? operator +(GenericEvent? left, IGenericAction<Entity> right)
    {
        if (right is not null)
        {
            if (left is null)
            {
                left = new();
                left._first = right;
            }
            else
            {
                left._invokationList.Push(right);
            }
        }
        return left;
    }

    public static GenericEvent? operator -(GenericEvent? left, IGenericAction<Entity> right)
    {
        if (left is not null && right is not null)
        {
            left.Remove(right);
        }
        return left;
    }
}