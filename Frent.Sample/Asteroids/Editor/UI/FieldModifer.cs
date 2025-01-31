using System;
using Microsoft.Xna.Framework;
using ImGuiNET;
using System.Collections.Generic;

namespace Frent.Sample.Asteroids.Editor.UI;

internal interface IFieldModifer
{
    public Entity Entity { set; }
    public ComponentField FieldToModify { set; }
    Type FieldType { get; }
    void UpdateUI();
}

internal abstract class FieldModifierBase<T> : IFieldModifer
{
    public FieldModifierBase() : this(EqualityComparer<T>.Default)
    {

    }

    public FieldModifierBase(EqualityComparer<T> comparer)
    {
        _comparer = comparer;
    }

    private Entity _entity;
    public Entity Entity
    {
        protected get => _entity;
        set
        {
            _entity = value;
            _cachedBox = null;
        }
    }

    private ComponentField? _field;
    public ComponentField? FieldToModify
    {
        protected get => _field;
        set
        {
            _field = value;
            _cachedBox = null;
        }
    }

    public Type FieldType => typeof(T);

    private readonly EqualityComparer<T> _comparer;
    private object? _cachedBox;
    protected T? _current;

    public void UpdateUI()
    {
        if (Entity.IsAlive && FieldToModify is not null)
        {
            //never actually ended up caching the box...
            _cachedBox = FieldToModify.GetValue(Entity.Get(FieldToModify.ComponentID));
            _current = (T)_cachedBox;

            T updatedValue = UpdateValue(FieldToModify);
            if (!EqualityComparer<T>.Default.Equals(updatedValue, _current))
            {
                _current = updatedValue;
                _cachedBox = updatedValue;
                FieldToModify.SetValue(Entity, _cachedBox!);
            }
        }
    }

    protected abstract T UpdateValue(ComponentField field);
}