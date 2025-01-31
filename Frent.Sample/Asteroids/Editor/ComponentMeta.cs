using System.Collections.Immutable;
using Frent.Core;
using Frent.Components;
using System.Linq;
using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Frozen;

namespace Frent.Sample.Asteroids.Editor;
internal class ComponentMeta(ComponentID id)
{
    public string Name { get; private set; } = id.Type.Name;
    public string? Description { get; private set; } = id.Type.GetCustomAttribute<DescriptionAttribute>()?.Description;
    public ComponentID ID { get; private set; } = id;

    public ImmutableArray<ComponentID> Arguments { get; private set; } =
        id.Type.GetMethod("Update")?
               .GetParameters()
               .Where(t => t.ParameterType.IsByRef)
               .Select(p => Component.GetComponentID(p.ParameterType.GetElementType()!))
               .ToImmutableArray()
            ?? [];

    public ImmutableArray<ComponentField> ComponentFields { get; set; } =
        Attribute.IsDefined(id.Type, typeof(EditorAttribute)) ?
            id.Type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(t => !Attribute.IsDefined(t, typeof(EditorExclude)))
                .Select(t => t switch
                {
                    FieldInfo field => new ComponentField(id, field),
                    PropertyInfo prop => new ComponentField(id, prop),
                    _ => null!
                })
                .Where(t => t is not null).ToImmutableArray()
            : id.Type
        .GetMembers(BindingFlags.Instance | BindingFlags.Public)
        .Where(t => Attribute.IsDefined(t, typeof(EditorAttribute)))
        .Select(t => t switch
        {
            FieldInfo field => new ComponentField(id, field),
            PropertyInfo prop => new ComponentField(id, prop),
            _ => null!
        })
        .Where(t => t is not null)
        .ToImmutableArray();

    public static readonly ImmutableArray<ComponentMeta> Components = typeof(ComponentMeta)
        .Assembly
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IComponentBase)) && Attribute.IsDefined(t, typeof(EditorAttribute)))
        .Select(t => new ComponentMeta(Component.GetComponentID(t)))
        .ToImmutableArray();

    public static readonly FrozenDictionary<ComponentID, ComponentMeta> ComponentMetaTable = Components.ToFrozenDictionary(k => k.ID);
}

internal class ComponentField
{
    public ComponentField(ComponentID id, FieldInfo info)
    {
        Type = info.FieldType;
        ComponentID = id;
        _fieldInfo = info;
    }

    public ComponentField(ComponentID id, PropertyInfo info)
    {
        Type = info.PropertyType;
        ComponentID = id;
        _propertyInfo = info;
    }

    public string Name => _fieldInfo?.Name ?? _propertyInfo?.Name ?? throw new UnreachableException();

    public ComponentID ComponentID { get; init; }
    public Type Type { get; init; }
    private FieldInfo? _fieldInfo;
    private PropertyInfo? _propertyInfo;

    public object GetValue(object component)
    {
        if (_fieldInfo is not null)
        {
            return _fieldInfo.GetValue(component)!;
        }

        return _propertyInfo!.GetValue(component)!;
    }

    public void SetValue(Entity entity, object value)
    {
        object component = entity.Get(ComponentID);

        if (_fieldInfo is not null)
        {
            _fieldInfo.SetValue(component, value);
        }
        else
        {
            _propertyInfo!.SetValue(component, value);
        }

        entity.Set(ComponentID, component);
    }
}